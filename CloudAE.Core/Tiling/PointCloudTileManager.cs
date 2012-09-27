using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Diagnostics;

using CloudAE.Core.Geometry;

namespace CloudAE.Core
{
	public class PointCloudTileManager : IPropertyContainer
	{
		public static readonly PropertyState<int> PROPERTY_DESIRED_TILE_COUNT;
		private static readonly PropertyState<int> PROPERTY_MAX_TILES_FOR_ESTIMATION;
		private static readonly PropertyState<bool> PROPERTY_COMPUTE_OPTIMAL_QUANTIZATION;

		private readonly IPointCloudBinarySource m_source;
		private readonly bool m_quantized;
		private readonly Func<IPointCloudBinarySource, PointBufferWrapper, Grid<int>, ProgressManager, PointCloudAnalysisResult> m_estimateDensityFunc;
		private readonly Func<IPointCloudBinarySource, PointBufferWrapper, Grid<int>, Quantization3D, ProgressManager, PointCloudTileDensity> m_initializeCountsFunc;
		private readonly Action<IPointCloudBinarySource, PointBufferWrapper, PointCloudTileSource, ProgressManager> m_tilePointsFunc;

		static PointCloudTileManager()
		{
			PROPERTY_DESIRED_TILE_COUNT = Context.RegisterOption(Context.OptionCategory.Tiling, "DesiredTilePoints", 40000);
			PROPERTY_MAX_TILES_FOR_ESTIMATION = Context.RegisterOption(Context.OptionCategory.Tiling, "EstimationTilesMax", 10000);
			PROPERTY_COMPUTE_OPTIMAL_QUANTIZATION = Context.RegisterOption(Context.OptionCategory.Tiling, "ComputeOptimalQuantization", true);
		}

		public PointCloudTileManager(IPointCloudBinarySource source)
		{
			m_source = source;
			m_quantized = (m_source.Quantization != null);

			if (m_quantized)
			{
				m_estimateDensityFunc  = QuantEstimateDensity;
				m_initializeCountsFunc = QuantInitializeCounts;
				m_tilePointsFunc       = QuantTilePoints;
			}
			else
			{
				m_estimateDensityFunc  = FloatEstimateDensity;
				m_initializeCountsFunc = FloatInitializeCounts;
				m_tilePointsFunc       = FloatTilePoints;
			}
		}

		public PointCloudTileSource TilePointFile(LASFile tiledFile, PointBufferWrapper segmentBuffer, ProgressManager progressManager)
		{
			var stopwatch = new Stopwatch();
			stopwatch.Start();

			// pass 1
			var analysis = AnalyzePointFile(segmentBuffer, progressManager);
			segmentBuffer = segmentBuffer.Initialize();

			return TilePointFileSegment(tiledFile, analysis, segmentBuffer, progressManager);
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

		public PointCloudAnalysisResult AnalyzePointFile(PointBufferWrapper segmentBuffer, ProgressManager progressManager)
		{
			var stopwatch = new Stopwatch();
			stopwatch.Start();

			var analysis = EstimateDensity(m_source, segmentBuffer, progressManager);
			progressManager.Log(stopwatch, "Computed Density ({0})", analysis.Density);

			return analysis;
		}

		#region Tiling Passes

		/// <summary>
		/// Estimates the points per square unit.
		/// </summary>
		/// <returns></returns>
		private PointCloudAnalysisResult EstimateDensity(IPointCloudBinarySource source, PointBufferWrapper segmentBuffer, ProgressManager progressManager)
		{
			var tileCounts = CreateTileCountsForEstimation(source);
			var density = m_estimateDensityFunc(source, segmentBuffer, tileCounts, progressManager);
			return density;
		}

		private PointCloudTileSet InitializeCounts(IPointCloudBinarySource source, PointCloudAnalysisResult analysis, PointBufferWrapper segmentBuffer, ProgressManager progressManager)
		{
			var tileCounts = analysis.Density.CreateTileCountsForInitialization(true);
			var actualDensity = m_initializeCountsFunc(source, segmentBuffer, tileCounts, analysis.Quantization, progressManager);
			var tileSet = new PointCloudTileSet(actualDensity, tileCounts);
			return tileSet;
		}

		private PointCloudTileSource TilePoints(IPointCloudBinarySource source, PointBufferWrapper segmentBuffer, LASFile file, PointCloudTileSet tileSet, PointCloudAnalysisResult analysis, ProgressManager progressManager)
		{
			if (File.Exists(file.FilePath))
				File.Delete(file.FilePath);

#warning this point size is incorrect for unquantized inputs
			var tileSource = new PointCloudTileSource(file, tileSet, analysis.Quantization, source.PointSizeBytes, analysis.Statistics);
			
			m_tilePointsFunc(source, segmentBuffer, tileSource, progressManager);

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

		#region Un-Quantized Methods

		private static PointCloudAnalysisResult FloatEstimateDensity(IPointCloudBinarySource source, PointBufferWrapper segmentBuffer, Grid<int> tileCounts, ProgressManager progressManager)
		{
			Statistics stats = null;
			Quantization3D quantization = null;

			var extent = source.Extent;

			QuantizationTest<double> quantizationTest = null;
			if (PROPERTY_COMPUTE_OPTIMAL_QUANTIZATION.Value)
				quantizationTest = new QuantizationTest<double>(source);

			using (var process = progressManager.StartProcess("FloatEstimateDensity"))
			{
				var statsMapping = new SimpleStatisticsMapping(extent.MinZ, extent.RangeZ, 1024);

				using (var gridCounter = new GridCounter(source, tileCounts))
				{
					foreach (var chunk in source.GetBlockEnumerator(process))
					{
						gridCounter.Process(chunk);
						statsMapping.Process(chunk);

						if (quantizationTest != null)
							quantizationTest.Process(chunk);
					}
				}

				stats = statsMapping.ComputeStatistics();

				if (quantizationTest != null)
					quantization = quantizationTest.CreateQuantization();
			}

			if (quantization == null)
				quantization = Quantization3D.Create(extent, true);

			var density = new PointCloudTileDensity(tileCounts, extent);
			var result = new PointCloudAnalysisResult(density, stats, quantization);

			return result;
		}

		private static unsafe PointCloudTileDensity FloatInitializeCounts(IPointCloudBinarySource source, PointBufferWrapper segmentBuffer, Grid<int> tileCounts, Quantization3D outputQuantization, ProgressManager progressManager)
		{
			var extent = source.Extent;
			var quantizedExtent = (UQuantizedExtent3D)outputQuantization.Convert(extent);

			double tilesOverRangeX = (double)tileCounts.SizeX / quantizedExtent.RangeX;
			double tilesOverRangeY = (double)tileCounts.SizeY / quantizedExtent.RangeY;

			using (var process = progressManager.StartProcess("FloatInitializeCounts"))
			{
				foreach (var chunk in source.GetBlockEnumerator(process))
				{
					byte* pb = chunk.PointDataPtr;
					while (pb < chunk.PointDataEndPtr)
					{
						Point3D* p = (Point3D*)pb;
						UQuantizedPoint3D* p2 = (UQuantizedPoint3D*)pb;

						// stomp on existing values since quantized is smaller
						(*p2).X = (uint)(((*p).X - outputQuantization.OffsetX) * outputQuantization.ScaleFactorInverseX);
						(*p2).Y = (uint)(((*p).Y - outputQuantization.OffsetY) * outputQuantization.ScaleFactorInverseY);

						++tileCounts.Data[
							(int)(((double)(*p2).X - quantizedExtent.MinX) * tilesOverRangeX),
							(int)(((double)(*p2).Y - quantizedExtent.MinY) * tilesOverRangeY)
						];

						pb += chunk.PointSizeBytes;
					}
				}

				tileCounts.CorrectCountOverflow();
			}

			var density = new PointCloudTileDensity(tileCounts, extent);
			return density;
		}

		private static unsafe void FloatTilePoints(IPointCloudBinarySource source, PointBufferWrapper segmentBuffer, PointCloudTileSource tileSource, ProgressManager progressManager)
		{
			var extent = source.Extent;
			var outputQuantization = tileSource.Quantization;
			var quantizedExtent = (UQuantizedExtent3D)outputQuantization.Convert(extent);
			var tileSet = tileSource.TileSet;

			double tilesOverRangeX = (double)tileSet.Cols / quantizedExtent.RangeX;
			double tilesOverRangeY = (double)tileSet.Rows / quantizedExtent.RangeY;

			using (var process = progressManager.StartProcess("FloatTilePoints"))
			{
				foreach (var chunk in source.GetBlockEnumerator(process))
				{
					byte* pb = chunk.PointDataPtr;
					while (pb < chunk.PointDataEndPtr)
					{
						Point3D* p = (Point3D*)pb;
						UQuantizedPoint3D* p2 = (UQuantizedPoint3D*)pb;

						(*p2).X = (uint)(((*p).X - outputQuantization.OffsetX) * outputQuantization.ScaleFactorInverseX);
						(*p2).Y = (uint)(((*p).Y - outputQuantization.OffsetY) * outputQuantization.ScaleFactorInverseY);
						(*p2).Z = (uint)(((*p).Z - outputQuantization.OffsetZ) * outputQuantization.ScaleFactorInverseZ);

						//tileBufferManager.AddPoint(pb,
						//    (int)(((double)(*p2).X - quantizedExtent.MinX) * tilesOverRangeX),
						//    (int)(((double)(*p2).Y - quantizedExtent.MinY) * tilesOverRangeY)
						//);

						pb += chunk.PointSizeBytes;
					}
				}
			}
		}

		#endregion

		#region Quantized Methods

		private static PointCloudAnalysisResult QuantEstimateDensity(IPointCloudBinarySource source, PointBufferWrapper segmentBuffer, Grid<int> tileCounts, ProgressManager progressManager)
		{
			Statistics stats = null;
			Quantization3D quantization = null;
			GridIndexSegments gridIndexSegments = null;

			var extent = source.Extent;
			var inputQuantization = (SQuantization3D)source.Quantization;
			var quantizedExtent = (SQuantizedExtent3D)inputQuantization.Convert(extent);

			QuantizationTest<int> quantizationTest = null;
			if (PROPERTY_COMPUTE_OPTIMAL_QUANTIZATION.Value)
				quantizationTest = new QuantizationTest<int>(source);

			GridIndexGenerator gridIndexGenerator = (segmentBuffer != null) ? null : new GridIndexGenerator();

			using (var process = progressManager.StartProcess("QuantEstimateDensity"))
			{
				var statsMapping = new ScaledStatisticsMapping(quantizedExtent.MinZ, quantizedExtent.RangeZ, 1024);

				using (var gridCounter = new GridCounter(source, tileCounts, gridIndexGenerator))
				{
					foreach (var chunk in source.GetBlockEnumerator(process))
					{
						gridCounter.Process(chunk);
						statsMapping.Process(chunk);

						if (quantizationTest != null)
							quantizationTest.Process(chunk);

						if (segmentBuffer != null)
							segmentBuffer.Append(chunk);
					}
				}

				stats = statsMapping.ComputeStatistics(extent.MinZ, extent.RangeZ);
				if (quantizationTest != null)
					quantization = quantizationTest.CreateQuantization();
			}

			if (quantization == null)
				quantization = Quantization3D.Create(extent, true);

			var density = new PointCloudTileDensity(tileCounts, extent);

			if (gridIndexGenerator != null)
				gridIndexSegments = gridIndexGenerator.GetGridIndex(density);

			var result = new PointCloudAnalysisResult(density, stats, quantization, gridIndexSegments);

			return result;
		}

		private static PointCloudTileDensity QuantInitializeCounts(IPointCloudBinarySource source, PointBufferWrapper segmentBuffer, Grid<int> tileCounts, Quantization3D outputQuantization, ProgressManager progressManager)
		{
			using (var process = progressManager.StartProcess("QuantInitializeCounts"))
			{
				using (var quantizationConverter = new QuantizationConverter(source, outputQuantization, tileCounts))
				{
					if (segmentBuffer.Initialized)
					{
						quantizationConverter.Process(segmentBuffer);
					}
					else
					{
						foreach (var chunk in source.GetBlockEnumerator(process))
						{
							quantizationConverter.Process(chunk);
							segmentBuffer.Append(chunk);
						}
					}
				}
			}

			var density = new PointCloudTileDensity(tileCounts, source.Extent);
			return density;
		}

		private static unsafe void QuantTilePoints(IPointCloudBinarySource source, PointBufferWrapper segmentBuffer, PointCloudTileSource tileSource, ProgressManager progressManager)
		{
			var extent = source.Extent;
			var outputQuantization = tileSource.Quantization;
			var quantizedExtent = (UQuantizedExtent3D)outputQuantization.Convert(extent);
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
						UQuantizedPoint3D* p = (UQuantizedPoint3D*)currentPosition.DataPtr;

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

			return new Grid<int>(tilesX, tilesY, extent, true);
		}

//        private static Grid<int> CreateTileCountsForInitialization(PointCloudTileDensity density)
//        {
//            Extent3D extent = density.Extent;

//            // median works better usually, but max is safer for substantially varying density
//            // (like terrestrial, although that requires a more thorough redesign)
//            //double tileArea = PROPERTY_DESIRED_TILE_COUNT.Value / density.MaxTileDensity;
//            double tileArea = PROPERTY_DESIRED_TILE_COUNT.Value / density.MedianTileDensity;
//            double tileSide = Math.Sqrt(tileArea);

//#warning this results in non-square tiles

//            ushort tilesX = (ushort)Math.Ceiling(extent.RangeX / tileSide);
//            ushort tilesY = (ushort)Math.Ceiling(extent.RangeY / tileSide);

//            return new Grid<int>(tilesX, tilesY, extent, true);
//        }

		#endregion
	}
}
