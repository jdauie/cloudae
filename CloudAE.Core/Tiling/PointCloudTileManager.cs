using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Jacere.Core;
using Jacere.Core.Geometry;
using Jacere.Core.Windows;
using Jacere.Data.PointCloud;
using ProcessPrivileges;

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
			PROPERTY_MAX_TILES_FOR_ESTIMATION = Context.RegisterOption(Context.OptionCategory.Tiling, "EstimationTilesMax", 10000000);
		}

		public PointCloudTileManager(IPointCloudBinarySource source)
		{
			m_source = source;
		}

		#region Indexed Tiling Methods

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

			long fileSize = tileSourceTemp.PointDataOffset + (m_source.PointSizeBytes * m_source.Count);

			AttemptFastAllocate(tiledFile.FilePath, fileSize);

			using (var outputStream = StreamManager.OpenWriteStream(tiledFile.FilePath, fileSize, tileSourceTemp.PointDataOffset))
			{
				int i = 0;
				foreach (var segment in analysis.GridIndex)
				{
					progressManager.Log("~ Processing Index Segment {0}/{1}", i + 1, analysis.GridIndex.Count);

					var sparseSegment = m_source.CreateSparseSegment(segment);
					var sparseSegmentWrapper = new PointBufferWrapper(segmentBuffer, sparseSegment);

					var tileRegionFilter = new TileRegionFilter(tileCounts, quantizedExtent, segment.GridRange);

					// this call will fill the buffer with points, add the counts, and sort
					QuantTilePointsIndexed(sparseSegment, sparseSegmentWrapper, tileRegionFilter, tileCounts, analysis.Quantization, progressManager);
					var segmentFilteredPointCount = tileRegionFilter.GetCellOrdering().Sum(t => tileCounts.Data[t.Row, t.Col]);
					var segmentFilteredBytes = segmentFilteredPointCount * sparseSegmentWrapper.PointSizeBytes;

					// write out the buffer
					using (var process = progressManager.StartProcess("WriteIndexSegment"))
					{
						//outputStream.Write(sparseSegmentWrapper.Data, 0, segmentFilteredPointCount * sparseSegmentWrapper.PointSizeBytes);
						int segmentBufferIndex = 0;
						foreach (var tileCount in segment.GridRange.GetCellOrdering().Select(c => tileCounts.Data[c.Row, c.Col]).Where(count => count > 0))
						{
							var tileSize = tileCount * sparseSegmentWrapper.PointSizeBytes;
							outputStream.Write(sparseSegmentWrapper.Data, segmentBufferIndex, tileSize);
							segmentBufferIndex += tileSize;

							if (!process.Update((float)segmentBufferIndex / segmentFilteredBytes))
								break;
						}
					}

					if (progressManager.IsCanceled())
						break;

					++i;
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

		private static void AttemptFastAllocate(string path, long fileSize)
		{
			var currentProcess = Process.GetCurrentProcess();
			using (new PrivilegeEnabler(currentProcess, Privilege.ManageVolume))
			{
				using (var outputStream = new FileStream(path, FileMode.OpenOrCreate))
				{
					outputStream.SetLength(fileSize);
					// (requires SE_MANAGE_VOLUME_NAME privilege, which is only given to admin by default)
					if (!NativeMethods.SetFileValidData(outputStream.SafeFileHandle.DangerousGetHandle(), fileSize))
					{
						Context.WriteLine("SetFileValidData() failed. Win32 error: {0}", Marshal.GetLastWin32Error());
					}
				}
			}
		}

		public PointCloudAnalysisResult AnalyzePointFile(int maxSegmentLength, ProgressManager progressManager)
		{
			var stopwatch = new Stopwatch();
			stopwatch.Start();

			var tileCounts = CreateTileCountsForEstimation(m_source);
			var analysis = QuantEstimateDensity(m_source, maxSegmentLength, tileCounts, progressManager);

			progressManager.Log(stopwatch, "Computed Density ({0})", analysis.Density);

			return analysis;
		}

		private static unsafe void QuantTilePointsIndexed(IPointCloudBinarySource source, PointBufferWrapper segmentBuffer, TileRegionFilter tileFilter, Grid<int> tileCounts, Quantization3D outputQuantization, ProgressManager progressManager)
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

			// sort points in buffer
			using (var process = progressManager.StartProcess("QuantTilePointsIndexedSort"))
			{
				var tilePositions = tileFilter.CreatePositionGrid(segmentBuffer, source.PointSizeBytes);

				int sortedCount = 0;
				foreach (var tile in tileFilter.GetCellOrdering())
				{
					var currentPosition = tilePositions[tile.Row, tile.Col];
					if (currentPosition.IsIncomplete)
					{
						while (currentPosition.IsIncomplete)
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

							++sortedCount;
						}

						if (!process.Update((float)sortedCount / segmentBuffer.PointCount))
							break;
					}
				}
			}
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

		#endregion

		#region Helpers

		private static Grid<int> CreateTileCountsForEstimation(IPointCloudBinarySource source)
		{
			long count = source.Count;
			Extent3D extent = source.Extent;

			int tileCountForUniformData = (int)(count / PROPERTY_DESIRED_TILE_COUNT.Value);
			//int tileCount = Math.Min(tileCountForUniformData, PROPERTY_MAX_TILES_FOR_ESTIMATION.Value);
			int tileCount = tileCountForUniformData;

			// for estimation, use a reduced tile size to minimize indexing overlap
			tileCount *= 16;

			tileCount = Math.Min(tileCount, PROPERTY_MAX_TILES_FOR_ESTIMATION.Value);

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
