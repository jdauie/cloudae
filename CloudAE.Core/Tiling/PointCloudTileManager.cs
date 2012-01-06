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
	public class PointCloudTileManager
	{
		private const int DEFAULT_DESIRED_TILE_COUNT = 40000;
		private const int DEFAULT_MAX_TILES_FOR_ESTIMATION = 10000;

		private static readonly PropertyState<int> PROPERTY_DESIRED_TILE_COUNT;
		private static readonly PropertyState<int> PROPERTY_MAX_TILES_FOR_ESTIMATION;

		private PointCloudBinarySource m_source;
		private PointCloudTileBufferManagerOptions m_options;

		static PointCloudTileManager()
		{
			PROPERTY_DESIRED_TILE_COUNT = Context.RegisterOption<int>(Context.OptionCategory.Tiling, "DesiredTileCount", DEFAULT_DESIRED_TILE_COUNT);
			PROPERTY_MAX_TILES_FOR_ESTIMATION = Context.RegisterOption<int>(Context.OptionCategory.Tiling, "EstimationTilesMax", DEFAULT_MAX_TILES_FOR_ESTIMATION);
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
		private static unsafe PointCloudTileDensity EstimateDensity(PointCloudBinarySource source, StatisticsGenerator statsGenerator, ProgressManager progressManager)
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

		private static unsafe PointCloudTileSet InitializeCounts(PointCloudBinarySource source, PointCloudTileDensity density, StatisticsGenerator statsGenerator, ProgressManager progressManager)
		{
			Grid<int> tileCounts = CreateTileCountsForInitialization(source, density);

			PointCloudTileDensity actualDensity = null;

			if (source.Quantization != null)
				actualDensity = CountPointsQuantized(source, tileCounts, statsGenerator, progressManager);
			else
				actualDensity = CountPoints(source, tileCounts, statsGenerator, progressManager);

			PointCloudTileSet tileSet = new PointCloudTileSet(actualDensity, tileCounts);

			//int[] testValidCounts = tileSet.Tiles.ToEnumerable<PointCloudTile>().Where(t => t.PointCount > 0).Select(t => t.PointCount).ToArray();
			//Array.Sort(testValidCounts);

			return tileSet;
		}

		private unsafe PointCloudTileSource TilePoints(PointCloudBinarySource source, string path, PointCloudTileSet tileSet, Statistics zStats, ProgressManager progressManager)
		{
			if (File.Exists(path))
				File.Delete(path);

			Quantization3D optimalQuantization = Quantization3D.Create(tileSet.Extent, true);
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

			tileSource.WriteHeader();

			return tileSource;
		}

		#endregion

		#region CountPoints

		private static unsafe PointCloudTileDensity CountPoints(PointCloudBinarySource source, Grid<int> tileCounts, StatisticsGenerator statsGenerator, ProgressManager progressManager)
		{
			bool hasMean = statsGenerator.HasMean;
			double sum = 0;

			int tilesX = tileCounts.SizeX;
			int tilesY = tileCounts.SizeY;

			Extent3D extent = source.Extent;

			double tilesOverRangeX = (double)tilesX / extent.RangeX;
			double tilesOverRangeY = (double)tilesY / extent.RangeY;

			byte[] buffer = BufferManager.AcquireBuffer();

			fixed (byte* inputBufferPtr = buffer)
			{
				foreach (PointCloudBinarySourceEnumeratorChunk chunk in source.GetEnumerator(buffer))
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
					}
					else
					{
						for (int i = 0; i < chunk.BytesRead; i += source.PointSizeBytes)
						{
							Point3D* p = (Point3D*)(inputBufferPtr + i);
							sum += (*p).Z;
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
				statsGenerator.SetVariance(sum / source.Count);
			else
				statsGenerator.SetMean(sum / source.Count);

			PointCloudTileDensity density = new PointCloudTileDensity(tileCounts, extent);
			return density;
		}

		private static unsafe PointCloudTileDensity CountPointsQuantized(PointCloudBinarySource source, Grid<int> tileCounts, StatisticsGenerator statsGenerator, ProgressManager progressManager)
		{
			bool hasMean = statsGenerator.HasMean;
			double sum = 0;

			Extent3D extent = source.Extent;
			SQuantization3D inputQuantization = (SQuantization3D)source.Quantization;
			SQuantizedExtent3D quantizedExtent = (SQuantizedExtent3D)inputQuantization.Convert(extent);

			double tilesOverRangeX = (double)tileCounts.SizeX / quantizedExtent.RangeX;
			double tilesOverRangeY = (double)tileCounts.SizeY / quantizedExtent.RangeY;

			//// test precision
			//int maxBytesForPrecisionTest = 1 << 24; // 26->64MB
			//int maxPointsForPrecisionTest = maxBytesForPrecisionTest / sizeof(SQuantizedPoint3D);
			//int pointsToTest = Math.Min(source.Count, maxPointsForPrecisionTest);

			//double[] scaleFactors = new double[] { inputQuantization.ScaleFactorX, inputQuantization.ScaleFactorY, inputQuantization.ScaleFactorZ };
			//int testValuesIndex = 0;
			//int[][] testValues = new int[3][];
			//for (int i = 0; i < 3; i++)
			//    testValues[i] = new int[pointsToTest];

			byte[] buffer = BufferManager.AcquireBuffer();

			fixed (byte* inputBufferPtr = buffer)
			{
				foreach (PointCloudBinarySourceEnumeratorChunk chunk in source.GetEnumerator(buffer))
				{
					for (int i = 0; i < chunk.BytesRead; i += source.PointSizeBytes)
					{
						SQuantizedPoint3D* p = (SQuantizedPoint3D*)(inputBufferPtr + i);

						//int tileX = (int)(((long)(*p).X - quantizedExtent.MinX) * tilesOverRangeX);
						//int tileY = (int)(((long)(*p).Y - quantizedExtent.MinY) * tilesOverRangeY);
						//++tileCounts.Data[tileX, tileY];

						++tileCounts.Data[
							(int)(((long)(*p).X - quantizedExtent.MinX) * tilesOverRangeX),
							(int)(((long)(*p).Y - quantizedExtent.MinY) * tilesOverRangeY)
						];
					}

					//if (testValuesIndex < pointsToTest)
					//{
					//    for (int i = 0; i < bytesRead; i += source.PointSizeBytes)
					//    {
					//        SQuantizedPoint3D* p = (SQuantizedPoint3D*)(inputBufferPtr + i);
					//        testValues[0][testValuesIndex] = (*p).X;
					//        testValues[1][testValuesIndex] = (*p).Y;
					//        testValues[2][testValuesIndex] = (*p).Z;
					//        ++testValuesIndex;
					//    }
					//}

					if (hasMean)
					{
						double offsetZMinusMean = inputQuantization.OffsetZ - statsGenerator.Mean;
						for (int i = 0; i < chunk.BytesRead; i += source.PointSizeBytes)
						{
							SQuantizedPoint3D* p = (SQuantizedPoint3D*)(inputBufferPtr + i);

							double differenceFromMean = ((*p).Z * inputQuantization.ScaleFactorZ + offsetZMinusMean);
							sum += differenceFromMean * differenceFromMean;
						}
					}
					else
					{
						for (int i = 0; i < chunk.BytesRead; i += source.PointSizeBytes)
						{
							SQuantizedPoint3D* p = (SQuantizedPoint3D*)(inputBufferPtr + i);
							sum += (*p).Z;
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
				double mean = (sum / source.Count) * inputQuantization.ScaleFactorZ + inputQuantization.OffsetZ;
				statsGenerator.SetMean(mean);
			}

			//SortedList<uint, int>[] testValueDifferenceCounts = new SortedList<uint, int>[3];
			//for (int i = 0; i < 3; i++)
			//{
			//    // sort precision test array
			//    //fixed (int* testValuesXPtr = testValues[i])
			//    //    QuickSort(testValuesXPtr, 0, pointsToTest - 1);

			//    int[] values = testValues[i];
			//    Array.Sort<int>(values);
			//    int min = values[0];
			//    int max = values[pointsToTest - 1];
			//    int range = max - min;

			//    // determine the base of the scale factor
			//    int scaleInverse = (int)Math.Ceiling(1 / scaleFactors[i]);
			//    int scaleBase = FindBase(scaleInverse);
			//    int scalePow = (int)Math.Log(scaleInverse, scaleBase);

			//    // count differences
			//    SortedList<uint, int> diffCounts = new SortedList<uint, int>();
			//    testValueDifferenceCounts[i] = diffCounts;

			//    for (int p = 1; p < pointsToTest; p++)
			//    {
			//        uint diff = (uint)(values[p] - values[p - 1]);
			//        if (diffCounts.ContainsKey(diff))
			//            ++diffCounts[diff];
			//        else
			//            diffCounts.Add(diff, 1);
			//    }

			//    // delta differences
			//    int differenceCount = diffCounts.Count;
			//    uint[] diffDiff = new uint[differenceCount];
			//    for (int d = 1; d < differenceCount; d++)
			//        diffDiff[d] = diffCounts.Keys[d] - diffCounts.Keys[d - 1];

			//    double[] diffDiffRatio = new double[differenceCount];
			//    for (int d = 1; d < differenceCount; d++)
			//        diffDiffRatio[d] = (double)diffDiff[d] / range;

			//    // determine power
			//    double[] diffDiffPow = new double[differenceCount];
			//    for (int d = 1; d < differenceCount; d++)
			//        diffDiffPow[d] = Math.Log(diffDiff[d], scaleBase);

			//    int minDiffPow = (int)Math.Ceiling(diffDiffPow.Skip(1).Max());

			//    if (minDiffPow > 1)
			//        scaleFactors[i] *= Math.Pow(scaleBase, minDiffPow);
			//}

			PointCloudTileDensity density = new PointCloudTileDensity(tileCounts, extent);
			return density;
		}

		#endregion

		private static int FindBase(int inverseScale)
		{
			// find factors
			Dictionary<int, int> factors = new Dictionary<int, int>();

			int currentFactorValue = 2;

			int remainder = inverseScale;
			while (remainder > 1)
			{
				if (remainder % currentFactorValue == 0)
				{
					remainder = remainder / currentFactorValue;
					if (factors.ContainsKey(currentFactorValue))
						++factors[currentFactorValue];
					else
						factors.Add(currentFactorValue, 1);
				}
				else
				{
					++currentFactorValue;
				}
			}

			int smallestCount = factors.Values.Min();

			int scaleBase = 1;
			foreach (int factor in factors.Keys)
			{
				scaleBase *= (factor * (factors[factor] / smallestCount));
			}

			return scaleBase;
		}

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

			return new Grid<int>(tilesX, tilesY, extent, true);
		}

		private static Grid<int> CreateTileCountsForInitialization(PointCloudBinarySource source, PointCloudTileDensity density)
		{
			Extent3D extent = source.Extent;

			// median works better usually, but max is safer for substantially varying density
			//double tileArea = MAX_TILE_POINTS / density.MaxTileDensity;
			double tileArea = PROPERTY_DESIRED_TILE_COUNT.Value / density.MedianTileDensity;
			double tileSide = Math.Sqrt(tileArea);

			ushort tilesX = (ushort)Math.Ceiling(extent.RangeX / tileSide);
			ushort tilesY = (ushort)Math.Ceiling(extent.RangeY / tileSide);

			return new Grid<int>(tilesX, tilesY, extent, true);
		}

		#endregion

		#region TilePointStream

		private static unsafe void TilePointStream(PointCloudBinarySource source, IPointCloudTileBufferManager tileBufferManager, ProgressManager progressManager)
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
				foreach (PointCloudBinarySourceEnumeratorChunk chunk in source.GetEnumerator(buffer))
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

		private static unsafe void TilePointStreamQuantized(PointCloudBinarySource source, IPointCloudTileBufferManager tileBufferManager, ProgressManager progressManager)
		{
			Quantization3D inputQuantization = source.Quantization;

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
				UQuantizedPoint3D point;
				foreach (PointCloudBinarySourceEnumeratorChunk chunk in source.GetEnumerator(buffer))
				{
					for (int i = 0; i < chunk.BytesRead; i += source.PointSizeBytes)
					{
						SQuantizedPoint3D* p = (SQuantizedPoint3D*)(inputBufferPtr + i);

						int tileX = (int)(((long)(*p).X - quantizedExtent.MinX) * tilesOverRangeX);
						if (tileX == tilesX) tileX = tilesX - 1;

						int tileY = (int)(((long)(*p).Y - quantizedExtent.MinY) * tilesOverRangeY);
						if (tileY == tilesY) tileY = tilesY - 1;

						point = new UQuantizedPoint3D(
							(uint)((*p).X * scaleTranslationX + offsetTranslationX),
							(uint)((*p).Y * scaleTranslationY + offsetTranslationY),
							(uint)((*p).Z * scaleTranslationZ + offsetTranslationZ)
						);

						tileBufferManager.AddPoint(point, tileX, tileY);
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
