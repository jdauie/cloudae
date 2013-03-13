using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Diagnostics;

using Jacere.Core;
using Jacere.Core.Geometry;
using Jacere.Data.PointCloud;

namespace CloudAE.Core
{
	public class PointCloudTileManager : IPropertyContainer
	{
		public static readonly IPropertyState<int> PROPERTY_DESIRED_TILE_COUNT;
		private static readonly IPropertyState<int> PROPERTY_MAX_TILES_FOR_ESTIMATION;

		private readonly IPointCloudBinarySource m_source;
        
		static PointCloudTileManager()
		{
			PROPERTY_DESIRED_TILE_COUNT = Context.RegisterOption(Context.OptionCategory.Tiling, "DesiredTilePoints", 40000);
			PROPERTY_MAX_TILES_FOR_ESTIMATION = Context.RegisterOption(Context.OptionCategory.Tiling, "EstimationTilesMax", 10000);
		}

		public PointCloudTileManager(IPointCloudBinarySource source)
		{
			m_source = source;
		}

		public PointCloudTileSource TilePointFileSegment(LASFile tiledFile, PointCloudAnalysisResult analysis, PointBufferWrapper segmentBuffer, ProgressManager progressManager)
		{
			var stopwatch = new Stopwatch();
			stopwatch.Start();

			// pass 2
			var tileSet = InitializeCounts(m_source, analysis, segmentBuffer, progressManager);
			progressManager.Log(stopwatch, "Computed Tile Offsets");

			// pass 3
			var tileSource = TilePoints(m_source, segmentBuffer, tiledFile, tileSet, analysis, progressManager);
			progressManager.Log(stopwatch, "Finished Tiling");

			return tileSource;
		}

		public PointCloudTileSource TilePointFileIndex(LASFile tiledFile, BufferInstance segmentBuffer, ProgressManager progressManager)
		{
			var stopwatch = new Stopwatch();
			stopwatch.Start();

			var analysis = AnalyzePointFile(segmentBuffer.Length, progressManager);
			var tileCounts = analysis.Density.CreateTileCountsForInitialization(true);

			var quantizedExtent = m_source.Quantization.Convert(analysis.Density.Extent);

			// create LAS output file (1.4 format)
			// (write header)
			// (copy VLRs)

			// create empty TileSet just to know the header size
			var densityTemp = new PointCloudTileDensity(tileCounts, m_source.Extent);
			var tileSetTemp = new PointCloudTileSet(densityTemp, tileCounts);
			var tileSourceTemp = new PointCloudTileSource(tiledFile, tileSetTemp, analysis.Quantization, m_source.PointSizeBytes, analysis.Statistics);


			using (var outputStream = StreamManager.OpenWriteStream(tiledFile.FilePath, tileSourceTemp.FileSize, tileSourceTemp.PointDataOffset))
			{
				foreach (var segment in analysis.GridIndex)
				{
					var sparseSegment = m_source.CreateSparseSegment(segment);
					var sparseSegmentWrapper = new PointBufferWrapper(segmentBuffer, sparseSegment);

					var tileRegionFilter = new TileRegionFilter(tileCounts, quantizedExtent, segment.GridRange);

					// this call will fill the buffer with points, add the counts, and sort
					int segmentFilteredPointCount = QuantTilePointsIndexed(sparseSegment, sparseSegmentWrapper, tileRegionFilter, tileCounts, analysis.Quantization, progressManager);

					// append to LAS file

					// next, write out the buffer
					outputStream.Write(sparseSegmentWrapper.Data, 0, segmentFilteredPointCount * sparseSegmentWrapper.PointSizeBytes);
				}
			}

			// at this point, counts have been completed
			// I can now make a tileSet and a tileSource
			// However, the tileSource constructor cannot stomp on the points that I have already written

			var actualDensity = new PointCloudTileDensity(tileCounts, m_source.Extent);
			var tileSet = new PointCloudTileSet(actualDensity, tileCounts);
			var tileSource = new PointCloudTileSource(tiledFile, tileSet, analysis.Quantization, m_source.PointSizeBytes, analysis.Statistics);

			if (!progressManager.IsCanceled())
				tileSource.IsDirty = false;

			tileSource.WriteHeader();

			return tileSource;
		}

		public PointCloudAnalysisResult AnalyzePointFile(int maxSegmentLength, ProgressManager progressManager)
		{
			var stopwatch = new Stopwatch();
			stopwatch.Start();

			var analysis = EstimateDensity(m_source, maxSegmentLength, progressManager);
			progressManager.Log(stopwatch, "Computed Density ({0})", analysis.Density);

			return analysis;
		}

		#region Tiling Passes

		/// <summary>
		/// Estimates the points per square unit.
		/// </summary>
		/// <returns></returns>
		private PointCloudAnalysisResult EstimateDensity(IPointCloudBinarySource source, int maxSegmentLength, ProgressManager progressManager)
		{
            var tileCounts = CreateTileCountsForEstimation(source);
			var density = QuantEstimateDensity(source, maxSegmentLength, tileCounts, progressManager);
			return density;
		}

		private PointCloudTileSet InitializeCounts(IPointCloudBinarySource source, PointCloudAnalysisResult analysis, PointBufferWrapper segmentBuffer, ProgressManager progressManager)
		{
			var tileCounts = analysis.Density.CreateTileCountsForInitialization(true);
            var actualDensity = QuantInitializeCounts(source, segmentBuffer, tileCounts, analysis.Quantization, progressManager);
			var tileSet = new PointCloudTileSet(actualDensity, tileCounts);
			return tileSet;
		}

		private PointCloudTileSource TilePoints(IPointCloudBinarySource source, PointBufferWrapper segmentBuffer, LASFile file, PointCloudTileSet tileSet, PointCloudAnalysisResult analysis, ProgressManager progressManager)
		{
			if (File.Exists(file.FilePath))
				File.Delete(file.FilePath);

#warning this point size is incorrect for unquantized inputs
			var tileSource = new PointCloudTileSource(file, tileSet, analysis.Quantization, source.PointSizeBytes, analysis.Statistics);

            QuantTilePoints(source, segmentBuffer, tileSource, progressManager);

			using (var process = progressManager.StartProcess("FinalizeTiles"))
			{
				using (var outputStream = StreamManager.OpenWriteStream(file.FilePath, tileSource.FileSize, tileSource.PointDataOffset))
				{
					var stopwatch = new Stopwatch();
					stopwatch.Start();
					int segmentBufferIndex = 0;
					foreach (var tile in tileSource.TileSet)
					{
						outputStream.Write(segmentBuffer.Data, segmentBufferIndex, tile.StorageSize);
						segmentBufferIndex += tile.StorageSize;

						if (!process.Update(tile))
							break;
					}
					stopwatch.Stop();
					double outputMBps = (double)outputStream.Position / (int)ByteSizesSmall.MB_1 * 1000 / stopwatch.ElapsedMilliseconds;
					Context.WriteLine("Write @ {0:0} MBps", outputMBps);
				}
			}

			if (!progressManager.IsCanceled())
				tileSource.IsDirty = false;

			tileSource.WriteHeader();

			return tileSource;
		}

		#endregion

		#region Quantized Methods

		private static unsafe int QuantTilePointsIndexed(IPointCloudBinarySource source, PointBufferWrapper segmentBuffer, TileRegionFilter tileFilter, Grid<int> tileCounts, Quantization3D outputQuantization, ProgressManager progressManager)
		{
			var quantizedExtent = source.Quantization.Convert(source.Extent);

			double tilesOverRangeX = (double)tileCounts.SizeX / quantizedExtent.RangeX;
			double tilesOverRangeY = (double)tileCounts.SizeY / quantizedExtent.RangeY;

			// generate counts and add points to buffer
			using (var process = progressManager.StartProcess("QuantTilePointsIndexedFilter"))
			{
				var group = new ChunkProcessSet(tileFilter, segmentBuffer);
				group.Process(source.GetBlockEnumerator(process));
			}

			int filteredPointCount = tileFilter.GetCellOrdering().Sum(tile => tileCounts.Data[tile.Row, tile.Col]);

			// sort points in buffer
			using (var process = progressManager.StartProcess("QuantTilePointsIndexedSort"))
			{
				var tilePositions = tileFilter.CreatePositionGrid(segmentBuffer, source.PointSizeBytes);

				int minSortedPointCount = 0;
				foreach (var tile in tileFilter.GetCellOrdering())
				{
					var currentPosition = tilePositions[tile.Row, tile.Col];

					while (currentPosition.DataPtr < currentPosition.DataEndPtr)
					{
						var p = (SQuantizedPoint3D*)currentPosition.DataPtr;

						var targetPosition = tilePositions[
							(int)(((double)(*p).Y - quantizedExtent.MinY) * tilesOverRangeY),
							(int)(((double)(*p).X - quantizedExtent.MinX) * tilesOverRangeX)
						];

						if (targetPosition.DataPtr != currentPosition.DataPtr)
						{
							// the point tile is not the current traversal tile,
							// so swap the points and resume on the swapped point
							targetPosition.Swap(currentPosition.DataPtr);
						}
						else
						{
							// this point is in the correct tile, move on
							currentPosition.Increment();
						}
					}

#warning progress is not working
					if (!process.Update((float)(currentPosition.DataPtr - segmentBuffer.PointDataPtr) / (segmentBuffer.Length)))
						break;
				}
			}

			return filteredPointCount;
		}

		private static PointCloudAnalysisResult QuantEstimateDensity(IPointCloudBinarySource source, int maxSegmentLength, Grid<int> tileCounts, ProgressManager progressManager)
		{
			Statistics stats = null;
			List<PointCloudBinarySourceEnumeratorSparseGridRegion> gridIndexSegments = null;

			var extent = source.Extent;
			var inputQuantization = source.Quantization;
			var quantizedExtent = inputQuantization.Convert(extent);

            var statsMapping = new ScaledStatisticsMapping(quantizedExtent.MinZ, quantizedExtent.RangeZ, 1024);
			var gridCounter = new GridCounter(source, tileCounts);

			using (var process = progressManager.StartProcess("QuantEstimateDensity"))
			{
				var group = new ChunkProcessSet(gridCounter, statsMapping);
				group.Process(source.GetBlockEnumerator(process));
			}

			stats = statsMapping.ComputeStatistics(extent.MinZ, extent.RangeZ);
			var density = new PointCloudTileDensity(tileCounts, extent);
			gridIndexSegments = gridCounter.GetGridIndex(density, maxSegmentLength);

			var result = new PointCloudAnalysisResult(density, stats, source.Quantization, gridIndexSegments);

			return result;
		}

		private static PointCloudTileDensity QuantInitializeCounts(IPointCloudBinarySource source, PointBufferWrapper segmentBuffer, Grid<int> tileCounts, Quantization3D outputQuantization, ProgressManager progressManager)
		{
			using (var process = progressManager.StartProcess("QuantInitializeCounts"))
			{
                //using (var quantizationConverter = new QuantizationConverter(source, outputQuantization, tileCounts))
                //{
					if (segmentBuffer.Initialized)
					{
						// small file (fit entirely within buffer)
						//quantizationConverter.Process(segmentBuffer);
					}
					else
					{
						// this is operating on one segment of a larger file
						foreach (var chunk in source.GetBlockEnumerator(process))
						{
							//quantizationConverter.Process(chunk);
							segmentBuffer.Append(chunk);
						}
					}
				//}
			}

			var density = new PointCloudTileDensity(tileCounts, source.Extent);
			return density;
		}

		private static unsafe void QuantTilePoints(IPointCloudBinarySource source, PointBufferWrapper segmentBuffer, PointCloudTileSource tileSource, ProgressManager progressManager)
		{
			var extent = source.Extent;
			var outputQuantization = tileSource.Quantization;
			var quantizedExtent = outputQuantization.Convert(extent);
			var tileSet = tileSource.TileSet;

			double tilesOverRangeX = (double)tileSet.Cols / quantizedExtent.RangeX;
			double tilesOverRangeY = (double)tileSet.Rows / quantizedExtent.RangeY;

			using (var process = progressManager.StartProcess("QuantTilePoints"))
			{
				var tilePositions = tileSet.CreatePositionGrid(segmentBuffer);

				foreach (PointCloudTile tile in tileSet)
				{
					var currentPosition = tilePositions[tile.Col, tile.Row];

					while (currentPosition.DataPtr < currentPosition.DataEndPtr)
					{
						var p = (SQuantizedPoint3D*)currentPosition.DataPtr;

						var targetPosition = tilePositions[
							(int)(((double)(*p).X - quantizedExtent.MinX) * tilesOverRangeX),
							(int)(((double)(*p).Y - quantizedExtent.MinY) * tilesOverRangeY)
						];

						if (targetPosition != currentPosition)
						{
							// the point tile is not the current traversal tile,
							// so swap the points and resume on the swapped point
							targetPosition.Swap(currentPosition.DataPtr);
						}
						else
						{
							// this point is in the correct tile, move on
							currentPosition.Increment();
						}
					}

					if (!process.Update(tile))
						break;
				}
			}
		}

		#endregion

		#region Helpers

		private static Grid<int> CreateTileCountsForEstimation(IPointCloudBinarySource source)
		{
			long count = source.Count;
			Extent3D extent = source.Extent;

			int tileCountForUniformData = (int)(count / PROPERTY_DESIRED_TILE_COUNT.Value);
			int tileCount = Math.Min(tileCountForUniformData, PROPERTY_MAX_TILES_FOR_ESTIMATION.Value);

			double tileArea = extent.Area / tileCount;
			double tileSide = Math.Sqrt(tileArea);

			ushort tilesX = (ushort)Math.Ceiling(extent.RangeX / tileSide);
			ushort tilesY = (ushort)Math.Ceiling(extent.RangeY / tileSide);

			if (tilesX == 0) tilesX = 1;
			if (tilesY == 0) tilesY = 1;

			return Grid<int>.CreateBuffered(tilesX, tilesY, extent);
		}

		#endregion
	}
}
