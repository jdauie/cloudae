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
		private static readonly PropertyState<ByteSizesLarge> PROPERTY_SEGMENT_SIZE;
		private static readonly PropertyState<bool> PROPERTY_REUSE_TILING;

		private readonly Identity m_id;

		private readonly FileHandlerBase m_inputHandler;
		private PointCloudBinarySource m_binarySource;
		private PointCloudTileSource m_tileSource;

		private readonly bool m_isInputPathLocal;
		private readonly string m_tiledPath;

		static ProcessingSet()
		{
			PROPERTY_SEGMENT_SIZE = Context.RegisterOption(Context.OptionCategory.Tiling, "MaxSegmentSize", ByteSizesLarge.GB_2);
			PROPERTY_REUSE_TILING = Context.RegisterOption(Context.OptionCategory.Tiling, "UseCache", true);
		}

		public ProcessingSet(FileHandlerBase inputFile)
		{
			m_id = IdentityManager.AcquireIdentity(GetType().Name);

			m_inputHandler = inputFile;
			m_isInputPathLocal = PathUtil.IsLocalPath(m_inputHandler.FilePath);
			m_tiledPath = PointCloudTileSource.GetTileSourcePath(m_inputHandler.FilePath);

			Directory.CreateDirectory(Path.GetDirectoryName(m_tiledPath));
		}

		public PointCloudTileSource Process(ProgressManager progressManager)
		{
			progressManager.Log("<= {0}", m_inputHandler.FilePath);

			// check for existing tile source
			LoadFromCache(progressManager);

			if (m_tileSource == null)
			{
				using (var process = progressManager.StartProcess("ProcessSet"))
				{
					// step 0
					if (!m_isInputPathLocal)
						CopyFileToLocalDrive(progressManager);

					// step 1
					m_binarySource = m_inputHandler.GenerateBinarySource(progressManager);

					// step 2,3
					var tileOptions = new PointCloudTileBufferManagerOptions();
					long pointDataSize = m_binarySource.Count * m_binarySource.PointSizeBytes;
					long maxSegmentBytes = (long)PROPERTY_SEGMENT_SIZE.Value;
					if (tileOptions.SupportsSegmentedProcessing && pointDataSize > maxSegmentBytes)
					{
						ProcessFileSegments(tileOptions, maxSegmentBytes, progressManager);
					}
					else
					{
						var tileManager = new PointCloudTileManager(m_binarySource, tileOptions);
						m_tileSource = tileManager.TilePointFile(m_tiledPath, progressManager);
					}

					GC.Collect();

					if (m_binarySource.FilePath != m_inputHandler.FilePath || !m_isInputPathLocal)
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

		private void ProcessFileSegments(PointCloudTileBufferManagerOptions tileOptions, long maxSegmentBytes, ProgressManager progressManager)
		{
			PointCloudBinarySource[] segments = null;
			PointCloudTileSource[] tiledSegments = null;
			PointCloudAnalysisResult analysis = null;

			{
				// check total point data size; try to keep it within windows file cache
				long pointDataSize = m_binarySource.Count * m_binarySource.PointSizeBytes;

				// determine density (for consistent tile size across chunks)
				var tileManager = new PointCloudTileManager(m_binarySource, tileOptions);
				analysis = tileManager.AnalyzePointFile(progressManager);

				int chunks = (int)Math.Ceiling((double)pointDataSize / maxSegmentBytes);
				long pointsPerChunk = m_binarySource.Count / chunks;
				long pointsRemaining = m_binarySource.Count;

				segments = new PointCloudBinarySource[chunks];
				for (int i = 0; i < chunks; i++)
				{
					long pointsInCurrentChunk = pointsPerChunk;
					if (i == chunks - 1)
						pointsInCurrentChunk = pointsRemaining;
					pointsRemaining -= pointsInCurrentChunk;

					segments[i] = new PointCloudBinarySourceSegment(m_binarySource, pointsPerChunk * i, pointsInCurrentChunk);
				}
			}

			BufferInstance segmentBuffer = BufferManager.AcquireBuffer(m_id, (int)maxSegmentBytes, true);

			// step 2
			using (var process = progressManager.StartProcess("ProcessTileSegments"))
			{
				// tile chunks seperately, using the same tile boundaries
				tiledSegments = new PointCloudTileSource[segments.Length];
				for (int i = 0; i < segments.Length; i++)
				{
					string tiledSegmentPath = GetTileSourcePath(m_binarySource.FilePath, i);
					process.Log("~ Processing Segment {0}/{1}", i + 1, segments.Length);

					var tileManager = new PointCloudTileManager(segments[i], tileOptions);
					tiledSegments[i] = tileManager.TilePointFile(tiledSegmentPath, analysis, segmentBuffer, progressManager);

					// why is random faster for parallel reads? RAID?
					//tiledSegments[i].OpenSequential();

					GC.Collect();

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
				int largestTileCount = mergedTileSet.Max(t => t.PointCount);
				int largestTileSize = largestTileCount * tiledSegments[0].PointSizeBytes;

				var tileSource = new PointCloudTileSource(m_tiledPath, mergedTileSet, tiledSegments[0].Quantization, tiledSegments[0].PointSizeBytes, tiledSegments[0].StatisticsZ);

				using (var inputBuffer = BufferManager.AcquireBuffer(m_id, largestTileSize, false))
				{
					using (var outputStream = new FileStreamUnbufferedSequentialWrite(m_tiledPath, tileSource.FileSize, tileSource.PointDataOffset))
					{
						int bytesInCurrentSegment = 0;
						int segmentBufferIndex = 0;

						// create buffer groups
						var segmentTileGroups = new List<List<PointCloudTile>>();
						var segmentTileGroupCurrent = new List<PointCloudTile>();
						segmentTileGroups.Add(segmentTileGroupCurrent);
						foreach (var tile in tileSource.TileSet.ValidTiles)
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

						// process buffer groups
						foreach (var group in segmentTileGroups)
						{
							// read tiles (ordered by segment)
							foreach (var segment in tiledSegments)
							{
								foreach (var tile in group)
								{
									var segmentTile = segment.TileSet[tile.Col, tile.Row];
									if (segmentTile.IsValid)
									{
										segmentTile.TileSource.LoadTile(segmentTile, inputBuffer.Data);
										Buffer.BlockCopy(inputBuffer.Data, 0, segmentBuffer.Data, segmentBufferIndex, segmentTile.StorageSize);
										segmentBufferIndex += segmentTile.StorageSize;
									}
								}
							}

							// flush buffer and reset
							outputStream.Write(segmentBuffer.Data, 0, segmentBufferIndex);
							segmentBufferIndex = 0;

							if (!process.Update((float)outputStream.Position / tileSource.FileSize))
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

		private void LoadFromCache(ProgressManager progressManager)
		{
			if (PROPERTY_REUSE_TILING.Value)
			{
				if (File.Exists(m_tiledPath))
				{
					progressManager.Log("Loading from Cache: {0}", Path.GetFileNameWithoutExtension(m_tiledPath));
					try
					{
						m_tileSource = PointCloudTileSource.Open(m_tiledPath);
					}
					catch
					{
						progressManager.Log("Cache Invalid; Regenerating.");
						File.Delete(m_tiledPath);
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
