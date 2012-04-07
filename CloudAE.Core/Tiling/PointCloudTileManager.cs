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

		private PointCloudBinarySource m_source;
		private PointCloudTileBufferManagerOptions m_options;

		private UQuantization3D m_testQuantization;

		static PointCloudTileManager()
		{
			PROPERTY_DESIRED_TILE_COUNT           = Context.RegisterOption<int>(Context.OptionCategory.Tiling, "DesiredTilePoints", 40000);
			PROPERTY_MAX_TILES_FOR_ESTIMATION     = Context.RegisterOption<int>(Context.OptionCategory.Tiling, "EstimationTilesMax", 10000);
			PROPERTY_COMPUTE_OPTIMAL_QUANTIZATION = Context.RegisterOption<bool>(Context.OptionCategory.Tiling, "ComputeOptimalQuantization", true);
			PROPERTY_QUANTIZATION_MEMORY_LIMIT    = Context.RegisterOption<ByteSizesSmall>(Context.OptionCategory.Tiling, "QuantizationMemoryLimit", ByteSizesSmall.MB_16);
		}
		
		public PointCloudTileManager(PointCloudBinarySource source, PointCloudTileBufferManagerOptions options)
		{
			m_source = source;
			m_options = options;
		}

		public PointCloudTileSource TilePointFile(string tiledPath, ProgressManager progressManager)
		{
			Stopwatch stopwatch = new Stopwatch();
			stopwatch.Start();

			// pass 1
			StatisticsGenerator statsGenerator = new StatisticsGenerator(m_source.Count);
			PointCloudTileDensity estimatedDensity = AnalyzePointFile(statsGenerator, progressManager);

			return TilePointFile(tiledPath, estimatedDensity, statsGenerator, progressManager);
		}

		public PointCloudTileSource TilePointFile(string tiledPath, PointCloudTileDensity estimatedDensity, StatisticsGenerator statsGenerator, ProgressManager progressManager)
		{
			Stopwatch stopwatch = new Stopwatch();
			stopwatch.Start();

			// pass 2
			PointCloudTileSet tileSet = InitializeCounts(m_source, estimatedDensity, statsGenerator, progressManager);
			progressManager.Log(stopwatch, "Computed Tile Offsets");

			// pass 3
			Statistics zStats = statsGenerator.Create();
			PointCloudTileSource tileSource = TilePoints(m_source, tiledPath, tileSet, zStats, progressManager);
			progressManager.Log(stopwatch, "Finished Tiling");

			return tileSource;
		}

		public PointCloudTileDensity AnalyzePointFile(StatisticsGenerator statsGenerator, ProgressManager progressManager)
		{
			Stopwatch stopwatch = new Stopwatch();
			stopwatch.Start();

			PointCloudTileDensity estimatedDensity = EstimateDensity(m_source, statsGenerator, progressManager);
			progressManager.Log(stopwatch, "Computed Density ({0})", estimatedDensity);

			return estimatedDensity;
		}

		#region Tiling Passes

		/// <summary>
		/// Estimates the points per square unit.
		/// </summary>
		/// <returns></returns>
		private unsafe PointCloudTileDensity EstimateDensity(PointCloudBinarySource source, StatisticsGenerator statsGenerator, ProgressManager progressManager)
		{
			Grid<int> tileCounts = CreateTileCountsForEstimation(source);
			
			PointCloudTileDensity density = null;

			if (source.Quantization != null)
				density = CountPointsQuantized(source, tileCounts, statsGenerator, progressManager);
			else
				density = CountPoints(source, tileCounts, statsGenerator, progressManager);

			//int[] testValidCounts = tileCounts.ToEnumerable<int>().Where(c => c > 0).ToArray();
			//Array.Sort(testValidCounts);

			return density;
		}

		private unsafe PointCloudTileSet InitializeCounts(PointCloudBinarySource source, PointCloudTileDensity density, StatisticsGenerator statsGenerator, ProgressManager progressManager)
		{
			Grid<int> tileCounts = CreateTileCountsForInitialization(source, density);

			PointCloudTileDensity actualDensity = null;

			if (source.Quantization != null)
				actualDensity = CountPointsQuantized(source, tileCounts, statsGenerator, progressManager);
			else
				actualDensity = CountPoints(source, tileCounts, statsGenerator, progressManager);

			PointCloudTileSet tileSet = new PointCloudTileSet(actualDensity, tileCounts, BufferManager.QUANTIZED_POINT_SIZE_BYTES);

			//int[] testValidCounts = tileSet.Tiles.ToEnumerable<PointCloudTile>().Where(t => t.PointCount > 0).Select(t => t.PointCount).ToArray();
			//Array.Sort(testValidCounts);

			return tileSet;
		}

		private unsafe PointCloudTileSource TilePoints(PointCloudBinarySource source, string path, PointCloudTileSet tileSet, Statistics zStats, ProgressManager progressManager)
		{
			if (File.Exists(path))
				File.Delete(path);

			Quantization3D optimalQuantization = m_testQuantization;
			if (optimalQuantization == null)
				optimalQuantization = Quantization3D.Create(tileSet.Extent, true);

			PointCloudTileSource tileSource = new PointCloudTileSource(path, tileSet, optimalQuantization, zStats, CompressionMethod.None);
			tileSource.AllocateFile(m_options.AllowSparseAllocation);

			using (FileStream outputStream = new FileStream(path, FileMode.Open, FileAccess.Write, FileShare.None, BufferManager.BUFFER_SIZE_BYTES, m_options.TilingFileOptions))
			{
				IPointCloudTileBufferManager tileBufferManager = m_options.CreateManager(tileSource, outputStream);

				if (source.Quantization != null)
					TilePointStreamQuantized(source, tileBufferManager, progressManager);
				else
					TilePointStream(source, tileBufferManager, progressManager);

				UQuantizedExtent3D newQuantizedExtent = tileBufferManager.FinalizeTiles(progressManager);
				tileSource.QuantizedExtent = newQuantizedExtent;
			}

			if (progressManager.Update(1.0f))
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

			int tilesX = tileCounts.SizeX;
			int tilesY = tileCounts.SizeY;

			Extent3D extent = source.Extent;

			double tilesOverRangeX = (double)tilesX / extent.RangeX;
			double tilesOverRangeY = (double)tilesY / extent.RangeY;

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

			byte[] buffer = BufferManager.AcquireBuffer();

			fixed (byte* inputBufferPtr = buffer)
			{
				foreach (PointCloudBinarySourceEnumeratorChunk chunk in source.GetBlockEnumerator(buffer))
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

					if (!progressManager.Update(chunk.EnumeratorProgress))
						break;
				}
			}

			BufferManager.ReleaseBuffer(buffer);

			// correct count overflows
			for (int x = 0; x <= tileCounts.SizeX; x++)
			{
				tileCounts.Data[x, tileCounts.SizeY - 1] += tileCounts.Data[x, tileCounts.SizeY];
				tileCounts.Data[x, tileCounts.SizeY] = 0;
			}
			for (int y = 0; y < tileCounts.SizeY; y++)
			{
				tileCounts.Data[tileCounts.SizeX - 1, y] += tileCounts.Data[tileCounts.SizeX, y];
				tileCounts.Data[tileCounts.SizeX, y] = 0;
			}

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
				int interval = verticalValueCounts.MaxIndex<long>();
				double mode = extent.MinZ + (double)interval / intervalsOverRangeZ;
				statsGenerator.SetMean(mean, mode);
			}

			if (testPrecision && hasMean)
			{
				m_testQuantization = Quantization3D.Create(extent, testValues);
			}

			PointCloudTileDensity density = new PointCloudTileDensity(tileCounts, extent);
			return density;
		}

		private unsafe PointCloudTileDensity CountPointsQuantized(PointCloudBinarySource source, Grid<int> tileCounts, StatisticsGenerator statsGenerator, ProgressManager progressManager)
		{
#warning replace variance mechanism
			// use http://www.johndcook.com/standard_deviation.html

			bool hasMean = statsGenerator.HasMean;
			double sum = 0;

			short pointSizeBytes = source.PointSizeBytes;
			Extent3D extent = source.Extent;
			SQuantization3D inputQuantization = (SQuantization3D)source.Quantization;
			SQuantizedExtent3D quantizedExtent = (SQuantizedExtent3D)inputQuantization.Convert(extent);

			double tilesOverRangeX = (double)tileCounts.SizeX / quantizedExtent.RangeX;
			double tilesOverRangeY = (double)tileCounts.SizeY / quantizedExtent.RangeY;

			int verticalValueIntervals = 256;
			long[] verticalValueCounts = new long[verticalValueIntervals + 1];
			float intervalsOverRangeZ = (float)verticalValueIntervals / quantizedExtent.RangeZ;

			// test precision
			bool testPrecision = PROPERTY_COMPUTE_OPTIMAL_QUANTIZATION.Value;
			int maxBytesForPrecisionTest = (int)PROPERTY_QUANTIZATION_MEMORY_LIMIT.Value;
			int maxPointsForPrecisionTest = maxBytesForPrecisionTest / sizeof(SQuantizedPoint3D);
			int pointsToTest = (int)Math.Min(source.Count, maxPointsForPrecisionTest);

			int testValuesIndex = 0;
			int[][] testValues = new int[3][];
			if (testPrecision)
				for (int i = 0; i < 3; i++)
					testValues[i] = new int[pointsToTest];

			using (ProgressManagerProcess process = progressManager.StartProcess("CountPointsQuantized"))
			{
				byte[] buffer = process.AcquireBuffer();

				fixed (byte* inputBufferPtr = buffer)
				{
					foreach (PointCloudBinarySourceEnumeratorChunk chunk in source.GetBlockEnumerator(buffer))
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

						if (hasMean)
						{
							double offsetZMinusMean = inputQuantization.OffsetZ - statsGenerator.Mean;

							pb = inputBufferPtr;
							while (pb < pbEnd)
							{
								SQuantizedPoint3D* p = (SQuantizedPoint3D*)(pb);
								pb += pointSizeBytes;

								double differenceFromMean = ((*p).Z * inputQuantization.ScaleFactorZ + offsetZMinusMean);
								sum += differenceFromMean * differenceFromMean;
							}

							if (testPrecision && testValuesIndex < pointsToTest)
							{
								pb = inputBufferPtr;
								while (pb < pbEnd)
								{
									if (testValuesIndex >= pointsToTest)
										break;

									SQuantizedPoint3D* p = (SQuantizedPoint3D*)(pb);
									pb += pointSizeBytes;
									testValues[0][testValuesIndex] = (*p).X;
									testValues[1][testValuesIndex] = (*p).Y;
									testValues[2][testValuesIndex] = (*p).Z;
									++testValuesIndex;
								}
							}
						}
						else
						{
							pb = inputBufferPtr;
							while (pb < pbEnd)
							{
								SQuantizedPoint3D* p = (SQuantizedPoint3D*)(pb);
								pb += pointSizeBytes;
								sum += (*p).Z;

								++verticalValueCounts[(int)(((float)(*p).Z - quantizedExtent.MinZ) * intervalsOverRangeZ)];
							}
						}

						if (!process.Update(chunk.EnumeratorProgress))
							break;
					}
				}

				// correct count overflows
				for (int x = 0; x <= tileCounts.SizeX; x++)
				{
					tileCounts.Data[x, tileCounts.SizeY - 1] += tileCounts.Data[x, tileCounts.SizeY];
					tileCounts.Data[x, tileCounts.SizeY] = 0;
				}
				for (int y = 0; y < tileCounts.SizeY; y++)
				{
					tileCounts.Data[tileCounts.SizeX - 1, y] += tileCounts.Data[tileCounts.SizeX, y];
					tileCounts.Data[tileCounts.SizeX, y] = 0;
				}

				// update stats
				if (hasMean)
				{
					statsGenerator.SetVariance(sum / source.Count);
				}
				else
				{
					double mean = (sum / source.Count) * inputQuantization.ScaleFactorZ + inputQuantization.OffsetZ;
					verticalValueCounts[verticalValueIntervals - 1] += verticalValueCounts[verticalValueIntervals];
					verticalValueCounts[verticalValueIntervals] = 0;
					int interval = verticalValueCounts.MaxIndex<long>();
					double mode = extent.MinZ + extent.RangeZ * interval / verticalValueIntervals;
					statsGenerator.SetMean(mean, mode);
				}

				if (testPrecision && hasMean)
				{
					m_testQuantization = Quantization3D.Create(extent, inputQuantization, testValues);
				}
			}

			PointCloudTileDensity density = new PointCloudTileDensity(tileCounts, extent);
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
			Extent3D extent = source.Extent;

			Quantization3D outputQuantization = tileBufferManager.TileSource.Quantization;
			PointCloudTileSet tileSet = tileBufferManager.TileSource.TileSet;
			int tilesX = tileSet.Cols;
			int tilesY = tileSet.Rows;

			double tilesOverRangeX = (double)tilesX / extent.RangeX;
			double tilesOverRangeY = (double)tilesY / extent.RangeY;

			byte[] buffer = BufferManager.AcquireBuffer();

			fixed (byte* inputBufferPtr = buffer)
			{
				foreach (PointCloudBinarySourceEnumeratorChunk chunk in source.GetBlockEnumerator(buffer))
				{
					for (int i = 0; i < chunk.BytesRead; i += source.PointSizeBytes)
					{
						Point3D* p = (Point3D*)(inputBufferPtr + i);

						int tileX = (int)(((*p).X - extent.MinX) * tilesOverRangeX);
						if (tileX == tilesX) tileX = tilesX - 1;

						int tileY = (int)(((*p).Y - extent.MinY) * tilesOverRangeY);
						if (tileY == tilesY) tileY = tilesY - 1;

						UQuantizedPoint3D uqPoint = new UQuantizedPoint3D(
							(uint)(((*p).X - outputQuantization.OffsetX) / outputQuantization.ScaleFactorX),
							(uint)(((*p).Y - outputQuantization.OffsetY) / outputQuantization.ScaleFactorY),
							(uint)(((*p).Z - outputQuantization.OffsetZ) / outputQuantization.ScaleFactorZ)
						);

						tileBufferManager.AddPoint(uqPoint, tileX, tileY);
					}

					if (!progressManager.Update(chunk.EnumeratorProgress))
						break;
				}
			}

			BufferManager.ReleaseBuffer(buffer);
		}

		private unsafe void TilePointStreamQuantized(PointCloudBinarySource source, IPointCloudTileBufferManager tileBufferManager, ProgressManager progressManager)
		{
			Quantization3D inputQuantization = source.Quantization;

			short pointSizeBytes = source.PointSizeBytes;
			Extent3D extent = source.Extent;
			SQuantizedExtent3D quantizedExtent = (SQuantizedExtent3D)inputQuantization.Convert(extent);

			Quantization3D outputQuantization = tileBufferManager.TileSource.Quantization;
			PointCloudTileSet tileSet = tileBufferManager.TileSource.TileSet;
			int tilesX = tileSet.Cols;
			int tilesY = tileSet.Rows;

			double tilesOverRangeX = (double)tilesX / quantizedExtent.RangeX;
			double tilesOverRangeY = (double)tilesY / quantizedExtent.RangeY;

			double scaleTranslationX = inputQuantization.ScaleFactorX / outputQuantization.ScaleFactorX;
			double scaleTranslationY = inputQuantization.ScaleFactorY / outputQuantization.ScaleFactorY;
			double scaleTranslationZ = inputQuantization.ScaleFactorZ / outputQuantization.ScaleFactorZ;

			double offsetTranslationX = (inputQuantization.OffsetX - outputQuantization.OffsetX) / outputQuantization.ScaleFactorX;
			double offsetTranslationY = (inputQuantization.OffsetY - outputQuantization.OffsetY) / outputQuantization.ScaleFactorY;
			double offsetTranslationZ = (inputQuantization.OffsetZ - outputQuantization.OffsetZ) / outputQuantization.ScaleFactorZ;

			byte[] buffer = BufferManager.AcquireBuffer();

			fixed (byte* inputBufferPtr = buffer)
			{
				foreach (PointCloudBinarySourceEnumeratorChunk chunk in source.GetBlockEnumerator(buffer))
				{
					byte* pb = inputBufferPtr;
					byte* pbEnd = inputBufferPtr + chunk.BytesRead;
					while (pb < pbEnd)
					{
						SQuantizedPoint3D* p = (SQuantizedPoint3D*)(pb);
						pb += pointSizeBytes;

						tileBufferManager.AddPoint(
							new UQuantizedPoint3D(
								(uint)((*p).X * scaleTranslationX + offsetTranslationX),
								(uint)((*p).Y * scaleTranslationY + offsetTranslationY),
								(uint)((*p).Z * scaleTranslationZ + offsetTranslationZ)
							),
							(int)(((double)(*p).X - quantizedExtent.MinX) * tilesOverRangeX),
							(int)(((double)(*p).Y - quantizedExtent.MinY) * tilesOverRangeY)
						);
					}

					if (!progressManager.Update(chunk.EnumeratorProgress))
						break;
				}
			}

			BufferManager.ReleaseBuffer(buffer);
		}

		#endregion
	}
}
