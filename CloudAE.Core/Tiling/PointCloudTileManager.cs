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
		private static readonly PropertyState<int> PROPERTY_DESIRED_TILE_COUNT;
		private static readonly PropertyState<int> PROPERTY_MAX_TILES_FOR_ESTIMATION;
		private static readonly PropertyState<bool> PROPERTY_COMPUTE_OPTIMAL_QUANTIZATION;
		
		private readonly PointCloudBinarySource m_source;
		private readonly bool m_quantized;
		private readonly Func<PointCloudBinarySource, PointBufferWrapper, Grid<int>, ProgressManager, PointCloudAnalysisResult> m_estimateDensityFunc;
		private readonly Func<PointCloudBinarySource, PointBufferWrapper, Grid<int>, Quantization3D, ProgressManager, PointCloudTileDensity> m_initializeCountsFunc;
		private readonly Action<PointCloudBinarySource, PointBufferWrapper, PointCloudTileSource, ProgressManager> m_tilePointsFunc;

		static PointCloudTileManager()
		{
			PROPERTY_DESIRED_TILE_COUNT = Context.RegisterOption(Context.OptionCategory.Tiling, "DesiredTilePoints", 40000);
			PROPERTY_MAX_TILES_FOR_ESTIMATION = Context.RegisterOption(Context.OptionCategory.Tiling, "EstimationTilesMax", 10000);
			PROPERTY_COMPUTE_OPTIMAL_QUANTIZATION = Context.RegisterOption(Context.OptionCategory.Tiling, "ComputeOptimalQuantization", true);
		}

		public PointCloudTileManager(PointCloudBinarySource source)
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

		public PointCloudTileSource TilePointFile(string tiledPath, PointBufferWrapper segmentBuffer, ProgressManager progressManager)
		{
			var stopwatch = new Stopwatch();
			stopwatch.Start();

			// pass 1
			var analysis = AnalyzePointFile(segmentBuffer, progressManager);
			segmentBuffer = segmentBuffer.Initialize();

			return TilePointFileSegment(tiledPath, analysis, segmentBuffer, progressManager);
		}

		public PointCloudTileSource TilePointFileSegment(string tiledPath, PointCloudAnalysisResult analysis, PointBufferWrapper segmentBuffer, ProgressManager progressManager)
		{
			var stopwatch = new Stopwatch();
			stopwatch.Start();

			// pass 2
			var tileSet = InitializeCounts(m_source, analysis, segmentBuffer, progressManager);
			progressManager.Log(stopwatch, "Computed Tile Offsets");

			// pass 3
			var tileSource = TilePoints(m_source, segmentBuffer, tiledPath, tileSet, analysis, progressManager);
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
		private PointCloudAnalysisResult EstimateDensity(PointCloudBinarySource source, PointBufferWrapper segmentBuffer, ProgressManager progressManager)
		{
			var tileCounts = CreateTileCountsForEstimation(source);
			var density = m_estimateDensityFunc(source, segmentBuffer, tileCounts, progressManager);
			return density;
		}

		private PointCloudTileSet InitializeCounts(PointCloudBinarySource source, PointCloudAnalysisResult analysis, PointBufferWrapper segmentBuffer, ProgressManager progressManager)
		{
			var tileCounts = CreateTileCountsForInitialization(source, analysis.Density);
			var actualDensity = m_initializeCountsFunc(source, segmentBuffer, tileCounts, analysis.Quantization, progressManager);
			var tileSet = new PointCloudTileSet(actualDensity, tileCounts);
			return tileSet;
		}

		private PointCloudTileSource TilePoints(PointCloudBinarySource source, PointBufferWrapper segmentBuffer, string path, PointCloudTileSet tileSet, PointCloudAnalysisResult analysis, ProgressManager progressManager)
		{
			if (File.Exists(path))
				File.Delete(path);

#warning this point size is incorrect for unquantized inputs
			var tileSource = new PointCloudTileSource(path, tileSet, analysis.Quantization, source.PointSizeBytes, analysis.Statistics);
			
			m_tilePointsFunc(source, segmentBuffer, tileSource, progressManager);

			using (var process = progressManager.StartProcess("FinalizeTiles"))
			{
				using (var outputStream = new FileStreamUnbufferedSequentialWrite(path, tileSource.FileSize, tileSource.PointDataOffset))
				{
					var stopwatch = new Stopwatch();
					stopwatch.Start();
					int segmentBufferIndex = 0;
					foreach (var tile in tileSource.TileSet.ValidTiles)
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

		private static unsafe PointCloudAnalysisResult FloatEstimateDensity(PointCloudBinarySource source, PointBufferWrapper segmentBuffer, Grid<int> tileCounts, ProgressManager progressManager)
		{
			bool computeStats = true;

			Statistics stats = null;
			Quantization3D quantization = null;

			short pointSizeBytes = source.PointSizeBytes;
			var extent = source.Extent;

			double tilesOverRangeX = tileCounts.SizeX / extent.RangeX;
			double tilesOverRangeY = tileCounts.SizeY / extent.RangeY;

			int verticalValueIntervals = 1024;
			long[] verticalValueCounts = new long[verticalValueIntervals + 1];
			float intervalsOverRangeZ = (float)(verticalValueIntervals / extent.RangeZ);

			// test precision
			QuantizationTest<double> qt = null;
			if (PROPERTY_COMPUTE_OPTIMAL_QUANTIZATION.Value)
				qt = new QuantizationTest<double>(source);

			using (var process = progressManager.StartProcess("CountPointsAnalysis"))
			{
				foreach (var chunk in source.GetBlockEnumerator(process))
				{
					byte* pb = chunk.DataPtr;
					byte* pbEnd = chunk.DataEndPtr;
					while (pb < pbEnd)
					{
						Point3D* p = (Point3D*)pb;
						++tileCounts.Data[
							(int)(((*p).X - extent.MinX) * tilesOverRangeX),
							(int)(((*p).Y - extent.MinY) * tilesOverRangeY)
						];
						pb += pointSizeBytes;
					}

					if (computeStats)
					{
						pb = chunk.DataPtr;
						while (pb < pbEnd)
						{
							Point3D* p = (Point3D*)pb;
							++verticalValueCounts[(int)(((*p).Z - extent.MinZ) * intervalsOverRangeZ)];
							pb += pointSizeBytes;
						}

						if (qt != null)
							qt.Process(chunk);
					}
				}
			}

			tileCounts.CorrectCountOverflow();

			if (computeStats)
			{
				stats = ScaledStatisticsMapping.ComputeStatistics(verticalValueCounts, true, extent.MinZ, extent.RangeZ);

				if (qt != null)
					quantization = qt.CreateQuantization();
			}

			if (quantization == null)
				quantization = Quantization3D.Create(source.Extent, true);

			var density = new PointCloudTileDensity(tileCounts, extent);
			var result = new PointCloudAnalysisResult(density, stats, quantization);

			return result;
		}

		private static unsafe PointCloudTileDensity FloatInitializeCounts(PointCloudBinarySource source, PointBufferWrapper segmentBuffer, Grid<int> tileCounts, Quantization3D outputQuantization, ProgressManager progressManager)
		{
			short pointSizeBytes = source.PointSizeBytes;
			var extent = source.Extent;
			var quantizedExtent = (UQuantizedExtent3D)outputQuantization.Convert(extent);

			double tilesOverRangeX = (double)tileCounts.SizeX / quantizedExtent.RangeX;
			double tilesOverRangeY = (double)tileCounts.SizeY / quantizedExtent.RangeY;

			using (var process = progressManager.StartProcess("CountPointsAccurate"))
			{
				foreach (var chunk in source.GetBlockEnumerator(process))
				{
					byte* pb = chunk.DataPtr;
					byte* pbEnd = chunk.DataEndPtr;
					while (pb < pbEnd)
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

						pb += pointSizeBytes;
					}
				}

				tileCounts.CorrectCountOverflow();
			}

			var density = new PointCloudTileDensity(tileCounts, extent);
			return density;
		}

		private static unsafe void FloatTilePoints(PointCloudBinarySource source, PointBufferWrapper segmentBuffer, PointCloudTileSource tileSource, ProgressManager progressManager)
		{
			short pointSizeBytes = source.PointSizeBytes;
			var extent = source.Extent;
			var outputQuantization = tileSource.Quantization;
			var quantizedExtent = (UQuantizedExtent3D)outputQuantization.Convert(extent);
			var tileSet = tileSource.TileSet;

			double tilesOverRangeX = (double)tileSet.Cols / quantizedExtent.RangeX;
			double tilesOverRangeY = (double)tileSet.Rows / quantizedExtent.RangeY;

			using (var process = progressManager.StartProcess("TilePointStream"))
			{
				foreach (var chunk in source.GetBlockEnumerator(process))
				{
					byte* pb = chunk.DataPtr;
					byte* pbEnd = chunk.DataEndPtr;
					while (pb < pbEnd)
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

						pb += pointSizeBytes;
					}
				}
			}
		}

		#endregion

		#region Quantized Methods

		private static unsafe PointCloudAnalysisResult QuantEstimateDensity(PointCloudBinarySource source, PointBufferWrapper segmentBuffer, Grid<int> tileCounts, ProgressManager progressManager)
		{
			Statistics stats = null;
			Quantization3D quantization = null;

			short pointSizeBytes = source.PointSizeBytes;
			var extent = source.Extent;
			var inputQuantization = (SQuantization3D)source.Quantization;
			var quantizedExtent = (SQuantizedExtent3D)inputQuantization.Convert(extent);

			double tilesOverRangeX = (double)tileCounts.SizeX / quantizedExtent.RangeX;
			double tilesOverRangeY = (double)tileCounts.SizeY / quantizedExtent.RangeY;

			ScaledStatisticsMapping vm = new ScaledStatisticsMapping(quantizedExtent.MinZ, quantizedExtent.RangeZ, 1024);
			QuantizationTest<int> qt = null;

			if (PROPERTY_COMPUTE_OPTIMAL_QUANTIZATION.Value)
				qt = new QuantizationTest<int>(source);

			int segmentBufferIndex = 0;

			using (var process = progressManager.StartProcess("CountPointsAnalysisQuantized"))
			{
				foreach (var chunk in source.GetBlockEnumerator(process))
				{
					byte* pb = chunk.DataPtr;
					byte* pbEnd = chunk.DataEndPtr;
					while (pb < pbEnd)
					{
						SQuantizedPoint3D* p = (SQuantizedPoint3D*)pb;

						int tileX = (int)(((double)(*p).X - quantizedExtent.MinX) * tilesOverRangeX);
						int tileY = (int)(((double)(*p).Y - quantizedExtent.MinY) * tilesOverRangeY);

						if (tileX < 0) tileX = 0; else if (tileX > tileCounts.SizeX) tileX = tileCounts.SizeX;
						if (tileY < 0) tileY = 0; else if (tileY > tileCounts.SizeY) tileY = tileCounts.SizeY;

						++tileCounts.Data[tileX, tileY];
						pb += pointSizeBytes;
					}

					vm.Process(chunk);
					if (qt != null)
						qt.Process(chunk);

					if (segmentBuffer != null)
					{
						Buffer.BlockCopy(chunk.Data, 0, segmentBuffer.Data, segmentBufferIndex, chunk.Length);
						segmentBufferIndex += chunk.Length;
					}
				}

				tileCounts.CorrectCountOverflow();

				stats = vm.ComputeStatistics(extent.MinZ, extent.RangeZ);
				if (qt != null)
					quantization = qt.CreateQuantization();
			}

			if (quantization == null)
				quantization = Quantization3D.Create(source.Extent, true);

			var density = new PointCloudTileDensity(tileCounts, extent);
			var result = new PointCloudAnalysisResult(density, stats, quantization);

			return result;
		}

		private static PointCloudTileDensity QuantInitializeCounts(PointCloudBinarySource source, PointBufferWrapper segmentBuffer, Grid<int> tileCounts, Quantization3D outputQuantization, ProgressManager progressManager)
		{
			var extent = source.Extent;
			var inputQuantization = (SQuantization3D)source.Quantization;
			var quantizedExtent = (UQuantizedExtent3D)outputQuantization.Convert(extent);

			var qc = new QuantizationConverter(inputQuantization, outputQuantization, quantizedExtent, tileCounts, source.PointSizeBytes);

			int segmentBufferIndex = 0;

			using (var process = progressManager.StartProcess("CountPointsAccurateQuantized"))
			{
				if (segmentBuffer.Initialized)
				{
					qc.Process(segmentBuffer);
				}
				else
				{
					// read from disk
					foreach (var chunk in source.GetBlockEnumerator(process))
					{
						qc.Process(chunk);

						Buffer.BlockCopy(chunk.Data, 0, segmentBuffer.Data, segmentBufferIndex, chunk.Length);
						segmentBufferIndex += chunk.Length;
					}
				}

				tileCounts.CorrectCountOverflow();
			}

			var density = new PointCloudTileDensity(tileCounts, extent);
			return density;
		}

		private static unsafe void QuantTilePoints(PointCloudBinarySource source, PointBufferWrapper segmentBuffer, PointCloudTileSource tileSource, ProgressManager progressManager)
		{
			var extent = source.Extent;
			var outputQuantization = tileSource.Quantization;
			var quantizedExtent = (UQuantizedExtent3D)outputQuantization.Convert(extent);
			var tileSet = tileSource.TileSet;

			double tilesOverRangeX = (double)tileSet.Cols / quantizedExtent.RangeX;
			double tilesOverRangeY = (double)tileSet.Rows / quantizedExtent.RangeY;

			using (var process = progressManager.StartProcess("TilePointStreamQuantized"))
			{
				var tilePositions = tileSet.CreatePositionGrid(segmentBuffer);

				foreach (PointCloudTile tile in tileSet.ValidTiles)
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

		private static Grid<int> CreateTileCountsForEstimation(PointCloudBinarySource source)
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

		private static Grid<int> CreateTileCountsForInitialization(PointCloudBinarySource source, PointCloudTileDensity density)
		{
			Extent3D extent = source.Extent;

			// median works better usually, but max is safer for substantially varying density
			// (like terrestrial, although that requires a more thorough redesign)
			//double tileArea = MAX_TILE_POINTS / density.MaxTileDensity;
			double tileArea = PROPERTY_DESIRED_TILE_COUNT.Value / density.MedianTileDensity;
			double tileSide = Math.Sqrt(tileArea);

#warning this results in non-square tiles

			ushort tilesX = (ushort)Math.Ceiling(extent.RangeX / tileSide);
			ushort tilesY = (ushort)Math.Ceiling(extent.RangeY / tileSide);

			return new Grid<int>(tilesX, tilesY, extent, true);
		}

		#endregion
	}
}
