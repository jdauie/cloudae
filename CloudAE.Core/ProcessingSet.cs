using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Diagnostics;

using CloudAE.Core.Util;

namespace CloudAE.Core
{
	public class ProcessingSet : IPropertyContainer
	{
		private static readonly IPropertyState<ByteSizesSmall> PROPERTY_SEGMENT_SIZE;
		private static readonly IPropertyState<bool> PROPERTY_REUSE_TILING;

		private readonly Identity m_id;

		private readonly FileHandlerBase m_inputHandler;
		private IPointCloudBinarySource m_binarySource;
		private PointCloudTileSource m_tileSource;

		private readonly bool m_isInputPathLocal;
		//private readonly string m_tiledPath;
		private readonly LASFile m_tiledHandler;

		static ProcessingSet()
		{
			PROPERTY_SEGMENT_SIZE = Context.RegisterOption(Context.OptionCategory.Tiling, "MaxSegmentSize", ByteSizesSmall.MB_256);
			PROPERTY_REUSE_TILING = Context.RegisterOption(Context.OptionCategory.Tiling, "UseCache", true);
		}

		public ProcessingSet(FileHandlerBase inputFile)
		{
			m_id = IdentityManager.AcquireIdentity(GetType().Name);

			m_inputHandler = inputFile;
			m_isInputPathLocal = PathUtil.IsLocalPath(m_inputHandler.FilePath);
			string tiledPath = PointCloudTileSource.GetTileSourcePath(m_inputHandler.FilePath);
			m_tiledHandler = new LASFile(tiledPath);

			Directory.CreateDirectory(Path.GetDirectoryName(m_tiledHandler.FilePath));
		}

		public PointCloudTileSource Process(ProgressManager progressManager)
		{
			progressManager.Log("<= {0}", m_inputHandler.FilePath);

			PerformanceManager.Start(m_inputHandler.FilePath);

			// check for existing tile source
			LoadFromCache(progressManager);

			if (m_tileSource == null)
			{
				using (var process = progressManager.StartProcess("ProcessSet"))
				{
					//// step 0
					//if (!m_isInputPathLocal)
					//    CopyFileToLocalDrive(progressManager);

					// step 1
					m_binarySource = m_inputHandler.GenerateBinarySource(progressManager);

					// step 2,3
					long pointDataSize = m_binarySource.Count * m_binarySource.PointSizeBytes;
					int maxSegmentBytes = (int)PROPERTY_SEGMENT_SIZE.Value;

					using (var segmentBuffer = BufferManager.AcquireBuffer(m_id, maxSegmentBytes, true))
					{
						if (pointDataSize > maxSegmentBytes)
						{
							ProcessFileSegments(segmentBuffer, progressManager);
						}
						else
						{
							var segmentWrapper = new PointBufferWrapper(segmentBuffer, m_binarySource);
							var tileManager = new PointCloudTileManager(m_binarySource);
							m_tileSource = tileManager.TilePointFile(m_tiledHandler, segmentWrapper, progressManager);

							if (!process.IsCanceled())
							{
								m_tileSource.IsDirty = false;
								m_tileSource.WriteHeader();
							}
						}
					}

					GC.Collect();

					//if (m_binarySource.FilePath != m_inputHandler.FilePath || !m_isInputPathLocal)
					//    File.Delete(m_binarySource.FilePath);

					if (m_binarySource.FilePath != m_inputHandler.FilePath)
						File.Delete(m_binarySource.FilePath);

					if (m_tileSource.IsDirty)
					{
						m_tileSource.Close();
						File.Delete(m_tileSource.FilePath);
						m_tileSource = null;

						process.LogTime("=> Processing Cancelled");
					}
					else
					{
						process.LogTime("=> Processing Completed");
					}
				}
			}

			TransferRate averageReadSpeed = PerformanceManager.GetReadSpeed();
			TransferRate averageWriteSpeed = PerformanceManager.GetWriteSpeed();

			Context.WriteLine("IO Read Speed: {0}", averageReadSpeed);
			Context.WriteLine("IO Write Speed: {0}", averageWriteSpeed);

			//{
			//    // test
			//    Stopwatch stopwatch = new Stopwatch();
			//    stopwatch.Start();

			//    PointCloudTile tempTile = m_tileSource.TileSet[0, 0];
			//    Grid<float> grid = new Grid<float>(tempTile.Extent, 540, (float)m_tileSource.Extent.MinZ - 1.0f, true);
			//    Grid<uint> quantizedGrid = new Grid<uint>(grid.SizeX, grid.SizeY, m_tileSource.Extent, true);

			//    using (GridTileSource<float> gridSource = new GridTileSource<float>(m_tiledPath + ".grid", grid.SizeX, grid.SizeY, m_tileSource.TileSet.Cols, m_tileSource.TileSet.Rows))
			//    {
			//        int tempBufferSize = (int)(m_tileSource.TileSet.Max(t => t.PointCount));
			//        byte[] tempBuffer = new byte[tempBufferSize * m_tileSource.PointSizeBytes];

			//        foreach (PointCloudTile tile in m_tileSource)
			//        {
			//            m_tileSource.LoadTileGrid(tile, tempBuffer, grid, quantizedGrid);
			//            gridSource.WriteTile(tile.Col, tile.Row, grid.Data);

			//            if (!progressManager.Update((float)tile.Index / m_tileSource.TileSet.TileCount))
			//                break;
			//        }

			//        //gridSource.ReadTile(tempTile.Col, tempTile.Row, grid.Data);
			//    }
			//    m_tileSource.Close();

			//    progressManager.Log(stopwatch, "Generated GRID");
			//}

			return m_tileSource;
		}

		private void ProcessFileSegments2(BufferInstance segmentBuffer, ProgressManager progressManager)
		{
			var tileManager = new PointCloudTileManager(m_binarySource);
			m_tileSource = tileManager.TilePointFileIndex(m_tiledHandler, segmentBuffer, progressManager);
		}

		private unsafe void ProcessFileSegments(BufferInstance segmentBuffer, ProgressManager progressManager)
		{
			IPointCloudBinarySource[] segments = null;
			PointCloudTileSource[] tiledSegments = null;
			PointCloudAnalysisResult analysis = null;

			using (StreamManager.CreateSharedStream(m_binarySource))
			{
				// step 1
				{
					// check total point data size; try to keep it within reasonable chunks
					long pointDataSize = m_binarySource.Count * m_binarySource.PointSizeBytes;

					// determine density (for consistent tile size across chunks)
					var tileManager = new PointCloudTileManager(m_binarySource);
					analysis = tileManager.AnalyzePointFile(null, progressManager);

					int chunks = (int)Math.Ceiling((double)pointDataSize / segmentBuffer.Length);
					long pointsPerChunk = m_binarySource.Count/chunks;
					long pointsRemaining = m_binarySource.Count;

					segments = new IPointCloudBinarySource[chunks];
					for (int i = 0; i < chunks; i++)
					{
						long pointsInCurrentChunk = pointsPerChunk;
						if (i == chunks - 1)
							pointsInCurrentChunk = pointsRemaining;
						pointsRemaining -= pointsInCurrentChunk;

						segments[i] = m_binarySource.CreateSegment(pointsPerChunk * i, pointsInCurrentChunk);
					}
				}

				// step 2
				using (var process = progressManager.StartProcess("ProcessTileSegments"))
				{
					// tile chunks seperately, using the same tile boundaries
					tiledSegments = new PointCloudTileSource[segments.Length];
					for (int i = 0; i < segments.Length; i++)
					{
						string tiledSegmentPath = GetTileSourcePath(m_binarySource.FilePath, i);
						var tiledSegmentHandler = new LASFile(tiledSegmentPath);
						process.Log("~ Processing Segment {0}/{1}", i + 1, segments.Length);

						var tileManager = new PointCloudTileManager(segments[i]);
						var segmentWrapper = new PointBufferWrapper(segmentBuffer, segments[i]);
						tiledSegments[i] = tileManager.TilePointFileSegment(tiledSegmentHandler, analysis, segmentWrapper, progressManager);

						// do I want to switch to an abort mechanism instead?
						//if (process.IsCanceled())
						//    break;
					}
				}

				// step 3
				using (var process = progressManager.StartProcess("MergeTileSegments"))
				{
					process.Log("Merging {0} Segments", segments.Length);

					// reassemble tiled files
					var mergedTileSet = new PointCloudTileSet(tiledSegments.Select(s => s.TileSet).ToArray());

					var tileSource = new PointCloudTileSource(m_tiledHandler, mergedTileSet, tiledSegments[0].Quantization, tiledSegments[0].PointSizeBytes, tiledSegments[0].StatisticsZ);

					using (var outputStream = StreamManager.OpenWriteStream(m_tiledHandler.FilePath, tileSource.FileSize, tileSource.PointDataOffset, true))
					{
						int bytesInCurrentSegment = 0;

						// create buffer groups
						var segmentTileGroups = new List<List<PointCloudTile>>();
						var segmentTileGroupCurrent = new List<PointCloudTile>();
						segmentTileGroups.Add(segmentTileGroupCurrent);
						foreach (var tile in tileSource.TileSet)
						{
							if (bytesInCurrentSegment + tile.StorageSize > segmentBuffer.Length)
							{
								segmentTileGroupCurrent = new List<PointCloudTile>();
								segmentTileGroups.Add(segmentTileGroupCurrent);
								bytesInCurrentSegment = 0;
							}
							segmentTileGroupCurrent.Add(tile);
							bytesInCurrentSegment += tile.StorageSize;
						}

						long totalToReadAndWrite = tileSource.PointSizeBytes * tileSource.Count * 2;
						long totalReadAndWritten = 0;

						// process buffer groups
						foreach (var group in segmentTileGroups)
						{
							int groupPoints = group.Sum(t => t.PointCount);
							//int groupLength = groupPoints * tileSource.PointSizeBytes;

							// read tiles (ordered by segment)
							foreach (var segment in tiledSegments)
							{
								// keep track of interleave location
								int mergedTileOffset = 0;
								foreach (var tile in group)
								{
									var segmentTile = segment.TileSet.GetTile(tile);
									if (segmentTile != null)
									{
										int previousSegmentTilesSize = tiledSegments.TakeWhile(s => s != segment).Select(s => s.TileSet.GetTile(tile)).Where(t => t != null).Sum(t => t.StorageSize);
										int segmentBufferIndex = mergedTileOffset + previousSegmentTilesSize;
										segmentTile.TileSet.TileSource.LoadTile(segmentTile, segmentBuffer.Data, segmentBufferIndex);
										totalReadAndWritten += segmentTile.StorageSize;
									}
									mergedTileOffset += tile.StorageSize;

									if (!process.Update((float)totalReadAndWritten / totalToReadAndWrite))
										break;
								}
							}

							// flush buffer and reset
							var segmentWrapper = new PointBufferWrapper(segmentBuffer, tileSource, groupPoints);
							foreach (var chunk in segmentWrapper)
							{
								outputStream.Write(chunk.PointDataPtr, chunk.Length);

								totalReadAndWritten += chunk.Length;
								if (!process.Update((float)totalReadAndWritten / totalToReadAndWrite))
									break;
							}
						}
					}

					for (int i = 0; i < tiledSegments.Length; i++)
					{
						tiledSegments[i].Close();
						File.Delete(tiledSegments[i].FilePath);
					}

					if (!process.IsCanceled())
					{
						tileSource.IsDirty = false;
						tileSource.WriteHeader();
					}

					m_tileSource = tileSource;

					process.LogTime("Merged Tiled Segments");
				}
			}
		}

		private void LoadFromCache(ProgressManager progressManager)
		{
			if (PROPERTY_REUSE_TILING.Value)
			{
				if (m_tiledHandler.Exists)
				{
					progressManager.Log("Loading from Cache: {0}", Path.GetFileNameWithoutExtension(m_tiledHandler.FilePath));
					try
					{
						m_tileSource = PointCloudTileSource.Open(m_tiledHandler);
					}
					catch
					{
						progressManager.Log("Cache Invalid; Regenerating.");
						File.Delete(m_tiledHandler.FilePath);
					}
				}
			}
		}

		private void CopyFileToLocalDrive(ProgressManager progressManager)
		{
			// if this is a network file, copy it to the local machine
			var stopwatch = new Stopwatch();
			stopwatch.Start();

			string dstPath = GetInputHandlerTempPath(m_inputHandler.FilePath);

			// apparently, I cannot disable buffering for network paths
			XCopy.Copy(m_inputHandler.FilePath, dstPath, true, false, 
				(o, pce) => progressManager.Update((float)pce.ProgressPercentage / 100)
			);

			m_inputHandler.FilePath = dstPath;

			progressManager.Log(stopwatch, "Copied Remote File");
		}

		public static string GetBinarySourceName(FileHandlerBase handler)
		{
			return string.Format("{0}.{1}", handler.FilePath, PointCloudBinarySource.FILE_EXTENSION);
		}

		public string GetInputHandlerTempPath(string path)
		{
			string fileName = String.Format("{0}", Path.GetFileName(m_inputHandler.FilePath));
			string tilePath = Path.Combine(Cache.APP_CACHE_DIR, fileName);
			return tilePath;
		}

		public string GetTileSourcePath(string path, int segmentIndex)
		{
			return string.Format("{0}{1}", PointCloudTileSource.GetTileSourcePath(path), segmentIndex);
		}

		public static string GetTemporaryCompressedTileSourceName(string path)
		{
			return path + ".tmp";
		}
	}
}
