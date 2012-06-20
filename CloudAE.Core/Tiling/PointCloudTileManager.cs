using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
		private static readonly PropertyState<ByteSizesSmall> PROPERTY_QUANTIZATION_MEMORY_LIMIT;

		private readonly PointCloudBinarySource m_source;
		private readonly PointCloudTileBufferManagerOptions m_options;

		static PointCloudTileManager()
		{
			PROPERTY_DESIRED_TILE_COUNT = Context.RegisterOption(Context.OptionCategory.Tiling, "DesiredTilePoints", 40000);
			PROPERTY_MAX_TILES_FOR_ESTIMATION = Context.RegisterOption(Context.OptionCategory.Tiling, "EstimationTilesMax", 10000);
			PROPERTY_COMPUTE_OPTIMAL_QUANTIZATION = Context.RegisterOption(Context.OptionCategory.Tiling, "ComputeOptimalQuantization", true);
			PROPERTY_QUANTIZATION_MEMORY_LIMIT = Context.RegisterOption(Context.OptionCategory.Tiling, "QuantizationMemoryLimit", ByteSizesSmall.MB_16);
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
			var analysis = AnalyzePointFile(progressManager);

			return TilePointFile(tiledPath, analysis, progressManager);
		}

		public PointCloudTileSource TilePointFile(string tiledPath, PointCloudAnalysisResult analysis, ProgressManager progressManager)
		{
			var stopwatch = new Stopwatch();
			stopwatch.Start();

			// pass 2
			var tileSet = InitializeCounts(m_source, analysis, progressManager);
			progressManager.Log(stopwatch, "Computed Tile Offsets");

			// pass 3
			var tileSource = TilePoints(m_source, tiledPath, tileSet, analysis, progressManager);
			progressManager.Log(stopwatch, "Finished Tiling");

			return tileSource;
		}

		public PointCloudAnalysisResult AnalyzePointFile(ProgressManager progressManager)
		{
			var stopwatch = new Stopwatch();
			stopwatch.Start();

			var analysis = EstimateDensity(m_source, progressManager);
			progressManager.Log(stopwatch, "Computed Density ({0})", analysis.Density);

			return analysis;
		}

		#region Tiling Passes

		/// <summary>
		/// Estimates the points per square unit.
		/// </summary>
		/// <returns></returns>
		private PointCloudAnalysisResult EstimateDensity(PointCloudBinarySource source, ProgressManager progressManager)
		{
			var tileCounts = CreateTileCountsForEstimation(source);

			PointCloudAnalysisResult density = null;

			if (source.Quantization != null)
				density = CountPointsQuantized(source, tileCounts, progressManager);
			//else
			//    density = CountPoints(source, tileCounts, statsGenerator, progressManager);

			return density;
		}

		private PointCloudTileSet InitializeCounts(PointCloudBinarySource source, PointCloudAnalysisResult analysis, ProgressManager progressManager)
		{
			var tileCounts = CreateTileCountsForInitialization(source, analysis.Density);

			PointCloudTileDensity actualDensity = null;

			// this no longer does stats (for quantized) -- refactor
			if (source.Quantization != null)
				actualDensity = CountPointsQuantized2(source, tileCounts, analysis.Quantization, progressManager);
			//else
			//    actualDensity = CountPoints(source, tileCounts, statsGenerator, progressManager);

			var tileSet = new PointCloudTileSet(actualDensity, tileCounts, source.PointSizeBytes);

			return tileSet;
		}

		private PointCloudTileSource TilePoints(PointCloudBinarySource source, string path, PointCloudTileSet tileSet, PointCloudAnalysisResult analysis, ProgressManager progressManager)
		{
			if (File.Exists(path))
				File.Delete(path);

			Statistics zStats = analysis.Statistics;
			Quantization3D quantization = analysis.Quantization;

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
				//broken
				//m_testQuantization = Quantization3D.Create(extent, testValues);
			}

			var density = new PointCloudTileDensity(tileCounts, extent);
			return density;
		}

		private unsafe PointCloudAnalysisResult CountPointsQuantized(PointCloudBinarySource source, Grid<int> tileCounts, ProgressManager progressManager)
		{
			bool computeStats = true;

			Statistics stats = null;
			Quantization3D quantization = null;

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
					stats = verticalValueMapping.ComputeStatistics(extent.MinZ, extent.RangeZ);

					if (testPrecision)
						quantization = Quantization3D.Create(extent, inputQuantization, testValues);
				}
			}

			if (quantization == null)
				quantization = Quantization3D.Create(source.Extent, true);

			var density = new PointCloudTileDensity(tileCounts, extent);
			var result = new PointCloudAnalysisResult(density, stats, quantization);

			return result;
		}

		private unsafe PointCloudTileDensity CountPointsQuantized2(PointCloudBinarySource source, Grid<int> tileCounts, Quantization3D outputQuantization, ProgressManager progressManager)
		{
			short pointSizeBytes = source.PointSizeBytes;
			var extent = source.Extent;
			var inputQuantization = (SQuantization3D)source.Quantization;
			var quantizedExtent = (UQuantizedExtent3D)outputQuantization.Convert(extent);

			double tilesOverRangeX = (double)tileCounts.SizeX / quantizedExtent.RangeX;
			double tilesOverRangeY = (double)tileCounts.SizeY / quantizedExtent.RangeY;

			double scaleTranslationX = inputQuantization.ScaleFactorX / outputQuantization.ScaleFactorX;
			double scaleTranslationY = inputQuantization.ScaleFactorY / outputQuantization.ScaleFactorY;

			double offsetTranslationX = (inputQuantization.OffsetX - outputQuantization.OffsetX) / outputQuantization.ScaleFactorX;
			double offsetTranslationY = (inputQuantization.OffsetY - outputQuantization.OffsetY) / outputQuantization.ScaleFactorY;

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
						UQuantizedPoint3D* p2 = (UQuantizedPoint3D*)(pb);
						
						// stomp on existing values since they are the same size
						(*p2).X = (uint)((*p).X * scaleTranslationX + offsetTranslationX);
						(*p2).Y = (uint)((*p).Y * scaleTranslationY + offsetTranslationY);

						++tileCounts.Data[
							(int)(((double)(*p2).X - quantizedExtent.MinX) * tilesOverRangeX),
							(int)(((double)(*p2).Y - quantizedExtent.MinY) * tilesOverRangeY)
						];

						pb += pointSizeBytes;
					}

					if (!process.Update(chunk))
						break;
				}

				tileCounts.CorrectCountOverflow();
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
			var outputQuantization = tileBufferManager.TileSource.Quantization;
			var quantizedExtent = (UQuantizedExtent3D)outputQuantization.Convert(extent);
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
						UQuantizedPoint3D* p2 = (UQuantizedPoint3D*)(pb);

						// stomp on existing values since they are the same size
						(*p2).X = (uint)((*p).X * scaleTranslationX + offsetTranslationX);
						(*p2).Y = (uint)((*p).Y * scaleTranslationY + offsetTranslationY);
						(*p2).Z = (uint)((*p).Z * scaleTranslationZ + offsetTranslationZ);

						//int tileX = (int)(((double)(*p2).X - quantizedExtent.MinX) * tilesOverRangeX);
						//int tileY = (int)(((double)(*p2).Y - quantizedExtent.MinY) * tilesOverRangeY);

						tileBufferManager.AddPoint(pb,
							(int)(((double)(*p2).X - quantizedExtent.MinX) * tilesOverRangeX), 
							(int)(((double)(*p2).Y - quantizedExtent.MinY) * tilesOverRangeY)
						);

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
