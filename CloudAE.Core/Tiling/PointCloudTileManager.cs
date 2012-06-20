using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Diagnostics;

using CloudAE.Core.Geometry;
using CloudAE.Core.Compression;

namespace CloudAE.Core
{
	public class PointCloudTileManager : IPropertyContainer
	{
		private static readonly PropertyState<int> PROPERTY_DESIRED_TILE_COUNT;
		private static readonly PropertyState<int> PROPERTY_MAX_TILES_FOR_ESTIMATION;
		private static readonly PropertyState<bool> PROPERTY_COMPUTE_OPTIMAL_QUANTIZATION;
		private static readonly PropertyState<ByteSizesSmall> PROPERTY_QUANTIZATION_MEMORY_LIMIT;

		private readonly PointCloudBinarySource m_source;
		private readonly PointCloudTileBufferManagerOptions m_options;

		private UQuantization3D m_testQuantization;

		static PointCloudTileManager()
		{
			PROPERTY_DESIRED_TILE_COUNT = Context.RegisterOption(Context.OptionCategory.Tiling, "DesiredTilePoints", 40000);
			PROPERTY_MAX_TILES_FOR_ESTIMATION = Context.RegisterOption(Context.OptionCategory.Tiling, "EstimationTilesMax", 10000);
			PROPERTY_COMPUTE_OPTIMAL_QUANTIZATION = Context.RegisterOption(Context.OptionCategory.Tiling, "ComputeOptimalQuantization", true);
			PROPERTY_QUANTIZATION_MEMORY_LIMIT = Context.RegisterOption(Context.OptionCategory.Tiling, "QuantizationMemoryLimit", ByteSizesSmall.MB_16);
		}

		public UQuantization3D TestQuantization
		{
			get { return m_testQuantization; }
		}

		public PointCloudTileManager(PointCloudBinarySource source, PointCloudTileBufferManagerOptions options)
		{
			m_source = source;
			m_options = options;
		}

		public PointCloudTileSource TilePointFile(string tiledPath, ProgressManager progressManager)
		{
			var stopwatch = new Stopwatch();
			stopwatch.Start();

			// pass 1
			var statsGenerator = new StatisticsGenerator(m_source.Count);
			var estimatedDensity = AnalyzePointFile(statsGenerator, progressManager);

			return TilePointFile(tiledPath, estimatedDensity, statsGenerator, m_testQuantization, progressManager);
		}

		public PointCloudTileSource TilePointFile(string tiledPath, PointCloudTileDensity estimatedDensity, StatisticsGenerator statsGenerator, Quantization3D quantization, ProgressManager progressManager)
		{
			var stopwatch = new Stopwatch();
			stopwatch.Start();

			// pass 2
			var tileSet = InitializeCounts(m_source, estimatedDensity, statsGenerator, progressManager);
			progressManager.Log(stopwatch, "Computed Tile Offsets");

			// pass 3
			var zStats = statsGenerator.Create();
			var tileSource = TilePoints(m_source, tiledPath, tileSet, zStats, quantization, progressManager);
			progressManager.Log(stopwatch, "Finished Tiling");

			return tileSource;
		}

		public PointCloudTileDensity AnalyzePointFile(StatisticsGenerator statsGenerator, ProgressManager progressManager)
		{
			var stopwatch = new Stopwatch();
			stopwatch.Start();

			var estimatedDensity = EstimateDensity(m_source, statsGenerator, progressManager);
			progressManager.Log(stopwatch, "Computed Density ({0})", estimatedDensity);

			return estimatedDensity;
		}

		#region Tiling Passes

		/// <summary>
		/// Estimates the points per square unit.
		/// </summary>
		/// <returns></returns>
		private PointCloudTileDensity EstimateDensity(PointCloudBinarySource source, StatisticsGenerator statsGenerator, ProgressManager progressManager)
		{
			var tileCounts = CreateTileCountsForEstimation(source);

			PointCloudTileDensity density = null;

			if (source.Quantization != null)
				density = CountPointsQuantized(source, tileCounts, statsGenerator, progressManager);
			else
				density = CountPoints(source, tileCounts, statsGenerator, progressManager);

			return density;
		}

		private PointCloudTileSet InitializeCounts(PointCloudBinarySource source, PointCloudTileDensity density, StatisticsGenerator statsGenerator, ProgressManager progressManager)
		{
			var tileCounts = CreateTileCountsForInitialization(source, density);

			PointCloudTileDensity actualDensity = null;

			// this no longer does stats (for quantized) -- refactor
			if (source.Quantization != null)
				actualDensity = CountPointsQuantized(source, tileCounts, null, progressManager);
			else
				actualDensity = CountPoints(source, tileCounts, statsGenerator, progressManager);

			var tileSet = new PointCloudTileSet(actualDensity, tileCounts, source.PointSizeBytes);

			return tileSet;
		}

		private PointCloudTileSource TilePoints(PointCloudBinarySource source, string path, PointCloudTileSet tileSet, Statistics zStats, Quantization3D quantization, ProgressManager progressManager)
		{
			if (File.Exists(path))
				File.Delete(path);

			if (quantization == null)
				quantization = Quantization3D.Create(tileSet.Extent, true);

#warning this point size is incorrect for unquantized inputs
			var tileSource = new PointCloudTileSource(path, tileSet, quantization, source.PointSizeBytes, zStats);
			tileSource.AllocateFile(m_options.AllowSparseAllocation);

			using (var outputStream = new FileStream(path, FileMode.Open, FileAccess.Write, FileShare.None, BufferManager.BUFFER_SIZE_BYTES, m_options.TilingFileOptions))
			{
				var tileBufferManager = m_options.CreateManager(tileSource, outputStream);

				if (source.Quantization != null)
					TilePointStreamQuantized(source, tileBufferManager, progressManager);
				else
					TilePointStream(source, tileBufferManager, progressManager);

				var newQuantizedExtent = tileBufferManager.FinalizeTiles(progressManager);
				tileSource.QuantizedExtent = newQuantizedExtent;
			}

			if (!progressManager.IsCanceled())
				tileSource.IsDirty = false;

			tileSource.WriteHeader();

			return tileSource;
		}

		#endregion

		#region CountPoints

		private unsafe PointCloudTileDensity CountPoints(PointCloudBinarySource source, Grid<int> tileCounts, StatisticsGenerator statsGenerator, ProgressManager progressManager)
		{
			bool hasMean = statsGenerator.HasMean;
			double sum = 0;

			var extent = source.Extent;

			double tilesOverRangeX = (double)tileCounts.SizeX / extent.RangeX;
			double tilesOverRangeY = (double)tileCounts.SizeY / extent.RangeY;

			int verticalValueIntervals = 256;
			long[] verticalValueCounts = new long[verticalValueIntervals + 1];
			float intervalsOverRangeZ = (float)(verticalValueIntervals / extent.RangeZ);

			// test precision
			bool testPrecision = PROPERTY_COMPUTE_OPTIMAL_QUANTIZATION.Value;
			int maxBytesForPrecisionTest = (int)PROPERTY_QUANTIZATION_MEMORY_LIMIT.Value;
			int maxPointsForPrecisionTest = maxBytesForPrecisionTest / sizeof(SQuantizedPoint3D);
			int pointsToTest = (int)Math.Min(source.Count, maxPointsForPrecisionTest);

			int testValuesIndex = 0;
			double[][] testValues = new double[3][];
			if (testPrecision)
				for (int i = 0; i < 3; i++)
					testValues[i] = new double[pointsToTest];

			using (var process = progressManager.StartProcess("CountPoints"))
			{
				BufferInstance buffer = process.AcquireBuffer(true);
				byte* inputBufferPtr = buffer.DataPtr;

				foreach (var chunk in source.GetBlockEnumerator(buffer.Data))
				{
					for (int i = 0; i < chunk.BytesRead; i += source.PointSizeBytes)
					{
						Point3D* p = (Point3D*)(inputBufferPtr + i);

						++tileCounts.Data[
							(int)(((*p).X - extent.MinX) * tilesOverRangeX),
							(int)(((*p).Y - extent.MinY) * tilesOverRangeY)
						];
					}

					if (hasMean)
					{
						for (int i = 0; i < chunk.BytesRead; i += source.PointSizeBytes)
						{
							Point3D* p = (Point3D*)(inputBufferPtr + i);
							double differenceFromMean = ((*p).Z - statsGenerator.Mean);
							sum += differenceFromMean * differenceFromMean;
						}

						if (testPrecision && testValuesIndex < pointsToTest)
						{
							for (int i = 0; i < chunk.BytesRead; i += source.PointSizeBytes)
							{
								if (testValuesIndex >= pointsToTest)
									break;

								Point3D* p = (Point3D*)(inputBufferPtr + i);
								testValues[0][testValuesIndex] = (*p).X;
								testValues[1][testValuesIndex] = (*p).Y;
								testValues[2][testValuesIndex] = (*p).Z;
								++testValuesIndex;
							}
						}
					}
					else
					{
						for (int i = 0; i < chunk.BytesRead; i += source.PointSizeBytes)
						{
							Point3D* p = (Point3D*)(inputBufferPtr + i);
							sum += (*p).Z;

							++verticalValueCounts[(int)(((*p).Z - extent.MinZ) * intervalsOverRangeZ)];
						}
					}

					if (!process.Update(chunk))
						break;
				}
			}

			tileCounts.CorrectCountOverflow();

			// update stats
			if (hasMean)
			{
				statsGenerator.SetVariance(sum / source.Count);
			}
			else
			{
				double mean = (sum / source.Count);
				verticalValueCounts[verticalValueIntervals - 1] += verticalValueCounts[verticalValueIntervals];
				verticalValueCounts[verticalValueIntervals] = 0;
				int interval = verticalValueCounts.MaxIndex();
				double mode = extent.MinZ + (double)interval / intervalsOverRangeZ;
				statsGenerator.SetMean(mean, mode);
			}

			if (testPrecision && hasMean)
			{
				m_testQuantization = Quantization3D.Create(extent, testValues);
			}

			var density = new PointCloudTileDensity(tileCounts, extent);
			return density;
		}

		private unsafe PointCloudTileDensity CountPointsQuantized(PointCloudBinarySource source, Grid<int> tileCounts, StatisticsGenerator statsGenerator, ProgressManager progressManager)
		{
			bool computeStats = (statsGenerator != null);

			short pointSizeBytes = source.PointSizeBytes;
			var extent = source.Extent;
			var inputQuantization = (SQuantization3D)source.Quantization;
			var quantizedExtent = (SQuantizedExtent3D)inputQuantization.Convert(extent);

			double tilesOverRangeX = (double)tileCounts.SizeX / quantizedExtent.RangeX;
			double tilesOverRangeY = (double)tileCounts.SizeY / quantizedExtent.RangeY;

			var verticalValueMapping = new ScaledStatisticsMapping(quantizedExtent.MinZ, quantizedExtent.RangeZ, 1024);
			long[] verticalValueCounts = verticalValueMapping.DestinationBins;
			int verticalValueRightShift = verticalValueMapping.SourceRightShift;
			int verticalValueMinShifted = verticalValueMapping.SourceMinShifted;

			// test precision
			bool testPrecision = PROPERTY_COMPUTE_OPTIMAL_QUANTIZATION.Value;
			int maxBytesForPrecisionTest = (int)PROPERTY_QUANTIZATION_MEMORY_LIMIT.Value;
			int maxPointsForPrecisionTest = maxBytesForPrecisionTest / sizeof(SQuantizedPoint3D);
			int maxPointsForPrecisionTestBlockAligned = (maxPointsForPrecisionTest / source.PointsPerBuffer) * source.PointsPerBuffer;
			int pointsToTest = (int)Math.Min(source.Count, maxPointsForPrecisionTestBlockAligned);

			int testValuesIndex = 0;
			int[][] testValues = new int[3][];
			if (computeStats && testPrecision)
			{
				for (int i = 0; i < 3; i++)
					testValues[i] = new int[pointsToTest];
			}

			using (var process = progressManager.StartProcess("CountPointsQuantized"))
			{
				BufferInstance buffer = process.AcquireBuffer(true);
				byte* inputBufferPtr = buffer.DataPtr;

				foreach (var chunk in source.GetBlockEnumerator(buffer.Data))
				{
					byte* pb = inputBufferPtr;
					byte* pbEnd = inputBufferPtr + chunk.BytesRead;
					while (pb < pbEnd)
					{
						SQuantizedPoint3D* p = (SQuantizedPoint3D*)(pb);
						pb += pointSizeBytes;

						++tileCounts.Data[
							(int)(((double)(*p).X - quantizedExtent.MinX) * tilesOverRangeX),
							(int)(((double)(*p).Y - quantizedExtent.MinY) * tilesOverRangeY)
						];
					}

					if (computeStats)
					{
						pb = inputBufferPtr;
						while (pb < pbEnd)
						{
							SQuantizedPoint3D* p = (SQuantizedPoint3D*)(pb);
							pb += pointSizeBytes;

							++verticalValueCounts[((*p).Z >> verticalValueRightShift) - verticalValueMinShifted];
						}

						if (testPrecision && testValuesIndex < pointsToTest)
						{
							pb = inputBufferPtr;
							while (pb < pbEnd)
							{
								SQuantizedPoint3D* p = (SQuantizedPoint3D*)(pb);
								pb += pointSizeBytes;
								testValues[0][testValuesIndex] = (*p).X;
								testValues[1][testValuesIndex] = (*p).Y;
								testValues[2][testValuesIndex] = (*p).Z;
								++testValuesIndex;
							}
						}
					}

					if (!process.Update(chunk))
						break;
				}

				tileCounts.CorrectCountOverflow();

				if (computeStats)
				{
					var stats = verticalValueMapping.ComputeStatistics(extent.MinZ, extent.RangeZ);
					statsGenerator.SetStatistics(stats.Mean, stats.Variance, stats.ModeApproximate);

					if (testPrecision)
						m_testQuantization = Quantization3D.Create(extent, inputQuantization, testValues);
				}
			}

			var density = new PointCloudTileDensity(tileCounts, extent);
			return density;
		}

		#endregion

		#region CreateTileCounts

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

			ushort tilesX = (ushort)Math.Ceiling(extent.RangeX / tileSide);
			ushort tilesY = (ushort)Math.Ceiling(extent.RangeY / tileSide);

			return new Grid<int>(tilesX, tilesY, extent, true);
		}

		#endregion

		#region TilePointStream

		private unsafe void TilePointStream(PointCloudBinarySource source, IPointCloudTileBufferManager tileBufferManager, ProgressManager progressManager)
		{
			short pointSizeBytes = source.PointSizeBytes;
			var extent = source.Extent;

			var outputQuantization = tileBufferManager.TileSource.Quantization;
			var tileSet = tileBufferManager.TileSource.TileSet;

			double tilesOverRangeX = (double)tileSet.Cols / extent.RangeX;
			double tilesOverRangeY = (double)tileSet.Rows / extent.RangeY;

			using (var process = progressManager.StartProcess("TilePointStream"))
			{
				BufferInstance buffer = process.AcquireBuffer(true);
				byte* inputBufferPtr = buffer.DataPtr;

				foreach (var chunk in source.GetBlockEnumerator(buffer.Data))
				{
					byte* pb = inputBufferPtr;
					byte* pbEnd = inputBufferPtr + chunk.BytesRead;
					while (pb < pbEnd)
					{
						Point3D* p = (Point3D*)pb;

						int tileX = (int)(((*p).X - extent.MinX) * tilesOverRangeX);
						int tileY = (int)(((*p).Y - extent.MinY) * tilesOverRangeY);

						UQuantizedPoint3D* p2 = (UQuantizedPoint3D*)pb;

						(*p2).X = (uint)(((*p).X - outputQuantization.OffsetX) / outputQuantization.ScaleFactorX);
						(*p2).Y = (uint)(((*p).Y - outputQuantization.OffsetY) / outputQuantization.ScaleFactorY);
						(*p2).Z = (uint)(((*p).Z - outputQuantization.OffsetZ) / outputQuantization.ScaleFactorZ);

						tileBufferManager.AddPoint(pb, tileX, tileY);

						pb += pointSizeBytes;
					}

					if (!process.Update(chunk))
						break;
				}
			}
		}

		private unsafe void TilePointStreamQuantized(PointCloudBinarySource source, IPointCloudTileBufferManager tileBufferManager, ProgressManager progressManager)
		{
			Quantization3D inputQuantization = source.Quantization;

			short pointSizeBytes = source.PointSizeBytes;
			var extent = source.Extent;
			var quantizedExtent = (SQuantizedExtent3D)inputQuantization.Convert(extent);

			var outputQuantization = tileBufferManager.TileSource.Quantization;
			var tileSet = tileBufferManager.TileSource.TileSet;

			double tilesOverRangeX = (double)tileSet.Cols / quantizedExtent.RangeX;
			double tilesOverRangeY = (double)tileSet.Rows / quantizedExtent.RangeY;

			double scaleTranslationX = inputQuantization.ScaleFactorX / outputQuantization.ScaleFactorX;
			double scaleTranslationY = inputQuantization.ScaleFactorY / outputQuantization.ScaleFactorY;
			double scaleTranslationZ = inputQuantization.ScaleFactorZ / outputQuantization.ScaleFactorZ;

			double offsetTranslationX = (inputQuantization.OffsetX - outputQuantization.OffsetX) / outputQuantization.ScaleFactorX;
			double offsetTranslationY = (inputQuantization.OffsetY - outputQuantization.OffsetY) / outputQuantization.ScaleFactorY;
			double offsetTranslationZ = (inputQuantization.OffsetZ - outputQuantization.OffsetZ) / outputQuantization.ScaleFactorZ;

			using (var process = progressManager.StartProcess("TilePointStreamQuantized"))
			{
				BufferInstance buffer = process.AcquireBuffer(true);
				byte* inputBufferPtr = buffer.DataPtr;

				foreach (var chunk in source.GetBlockEnumerator(buffer.Data))
				{
					byte* pb = inputBufferPtr;
					byte* pbEnd = inputBufferPtr + chunk.BytesRead;
					while (pb < pbEnd)
					{
						SQuantizedPoint3D* p = (SQuantizedPoint3D*)(pb);

						int tileX = (int)(((double)(*p).X - quantizedExtent.MinX) * tilesOverRangeX);
						int tileY = (int)(((double)(*p).Y - quantizedExtent.MinY) * tilesOverRangeY);

						UQuantizedPoint3D* p2 = (UQuantizedPoint3D*)(pb);

						// stomp on existing values since they are the same size
						(*p2).X = (uint)((*p).X * scaleTranslationX + offsetTranslationX);
						(*p2).Y = (uint)((*p).Y * scaleTranslationY + offsetTranslationY);
						(*p2).Z = (uint)((*p).Z * scaleTranslationZ + offsetTranslationZ);

						tileBufferManager.AddPoint(pb, tileX, tileY);

						pb += pointSizeBytes;
					}

					if (!process.Update(chunk))
						break;
				}
			}
		}

		#endregion
	}
}
