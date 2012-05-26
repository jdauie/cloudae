using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Diagnostics;

using CloudAE.Core.Util;
using CloudAE.Core.Compression;
using CloudAE.Core.Geometry;

namespace CloudAE.Core
{
	public class ProcessingSet : IPropertyContainer
	{
		private static readonly PropertyState<PointCloudTileBufferManagerMode> PROPERTY_TILE_MODE;
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
			PROPERTY_TILE_MODE = Context.RegisterOption<PointCloudTileBufferManagerMode>(Context.OptionCategory.Tiling, "Mode", PointCloudTileBufferManagerMode.OptimizeForLargeFile);
			PROPERTY_SEGMENT_SIZE = Context.RegisterOption<ByteSizesLarge>(Context.OptionCategory.Tiling, "MaxSegmentSize", ByteSizesLarge.GB_2);
			PROPERTY_REUSE_TILING = Context.RegisterOption<bool>(Context.OptionCategory.Tiling, "UseCache", true);
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
				using (ProgressManagerProcess process = progressManager.StartProcess("ProcessSet"))
				{
					// step 0
					if (!m_isInputPathLocal)
						CopyFileToLocalDrive(progressManager);

					// step 1
					m_binarySource = m_inputHandler.GenerateBinarySource(progressManager);

					// step 2,3
					PointCloudTileBufferManagerOptions tileOptions = new PointCloudTileBufferManagerOptions(PROPERTY_TILE_MODE.Value);
					long pointDataSize = (long)m_binarySource.Count * m_binarySource.PointSizeBytes;
					long maxSegmentBytes = (long)PROPERTY_SEGMENT_SIZE.Value;
					if (tileOptions.SupportsSegmentedProcessing && pointDataSize > maxSegmentBytes)
					{
						ProcessFileSegments(tileOptions, maxSegmentBytes, progressManager);
					}
					else
					{
						PointCloudTileManager tileManager = new PointCloudTileManager(m_binarySource, tileOptions);
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
						// step 4
						// this is just for testing at present
						//CompressTileSource(progressManager);

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
			StatisticsGenerator statsGenerator = null;
			PointCloudTileDensity estimatedDensity = null;
			UQuantization3D quantization = null;

			{
				// check total point data size; try to keep it within windows file cache
				long pointDataSize = (long)m_binarySource.Count * m_binarySource.PointSizeBytes;

				// determine density (for consistent tile size across chunks)
				statsGenerator = new StatisticsGenerator(m_binarySource.Count);
				PointCloudTileManager tileManager = new PointCloudTileManager(m_binarySource, tileOptions);
				estimatedDensity = tileManager.AnalyzePointFile(statsGenerator, progressManager);
				quantization = tileManager.TestQuantization;

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

			// step 2
			using (ProgressManagerProcess process = progressManager.StartProcess("ProcessTileSegments"))
			{
				// tile chunks seperately, using the same tile boundaries
				tiledSegments = new PointCloudTileSource[segments.Length];
				for (int i = 0; i < segments.Length; i++)
				{
					string tiledSegmentPath = GetTileSourcePath(m_binarySource.FilePath, i);
					process.Log("~ Processing Segment {0}/{1}", i + 1, segments.Length);

					PointCloudTileManager tileManager = new PointCloudTileManager(segments[i], tileOptions);
					tiledSegments[i] = tileManager.TilePointFile(tiledSegmentPath, estimatedDensity, statsGenerator, quantization, progressManager);

					// why is random faster for parallel reads? RAID?
					//tiledSegments[i].OpenSequential();

					GC.Collect();

					// do I want to switch to an abort mechanism instead?
					//if (process.IsCanceled())
					//    break;
				}
			}

			// step 3
			using (ProgressManagerProcess process = progressManager.StartProcess("MergeTileSegments"))
			{
				process.Log("Merging {0} Segments", segments.Length);

				// reassemble tiled files
				PointCloudTileSet mergedTileSet = new PointCloudTileSet(tiledSegments.Select(s => s.TileSet).ToArray());
				int largestTileCount = (int)(mergedTileSet.Max(t => t.PointCount));
				int largestTileSize = largestTileCount * tiledSegments[0].PointSizeBytes;
				BufferInstance inputBuffer = BufferManager.AcquireBuffer(m_id, largestTileSize, false);

				//byte[] largeBuffer = new byte[(int)ByteSizesSmall.MB_256];
				//int largeBufferPos = 0;

				PointCloudTileSource tileSource = new PointCloudTileSource(m_tiledPath, mergedTileSet, tiledSegments[0].Quantization, tiledSegments[0].PointSizeBytes, tiledSegments[0].StatisticsZ, CompressionMethod.None);

				tileSource.AllocateFile(tileOptions.AllowSparseAllocation);

				using (FileStream outputStream = new FileStream(m_tiledPath, FileMode.Open, FileAccess.Write, FileShare.None, BufferManager.BUFFER_SIZE_BYTES, tileOptions.TilingFileOptions))
				{
					outputStream.Seek(tileSource.PointDataOffset, SeekOrigin.Begin);

					// go through tiles and write at the correct offset
					foreach (PointCloudTile tile in tileSource.TileSet.ValidTiles)
					{
						for (int i = 0; i < tiledSegments.Length; i++)
						{
							PointCloudTile segmentTile = tiledSegments[i].TileSet[tile.Col, tile.Row];
							if (segmentTile.IsValid)
							{
								tiledSegments[i].LoadTile(segmentTile, inputBuffer.Data);
								outputStream.Write(inputBuffer.Data, 0, segmentTile.StorageSize);

								//if (largeBufferPos + segmentTile.StorageSize > largeBuffer.Length)
								//{
								//    outputStream.Write(largeBuffer, 0, largeBufferPos);
								//    largeBufferPos = 0;
								//}

								//Buffer.BlockCopy(inputBuffer, 0, largeBuffer, largeBufferPos, segmentTile.StorageSize);
								//largeBufferPos += segmentTile.StorageSize;
							}
						}
						if (!process.Update(tile))
							break;
					}

					//if (largeBufferPos > 0)
					//    outputStream.Write(largeBuffer, 0, largeBufferPos);
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
						// I should update the header reader/writer to have a dirty flag (to make sure the file wasn't partially written)
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
			Stopwatch stopwatch = new Stopwatch();
			stopwatch.Start();

			string dstPath = GetInputHandlerTempPath(m_inputHandler.FilePath);

			// apparently, I cannot disable buffering for network paths
			XCopy.Copy(m_inputHandler.FilePath, dstPath, true, false, (o, pce) =>
			{
				progressManager.Update((float)pce.ProgressPercentage / 100);
			});

			m_inputHandler.FilePath = dstPath;

			progressManager.Log(stopwatch, "Copied Remote File");
		}

		private PointCloudTileSource CompressTileSource(ProgressManager progressManager)
		{
			m_tileSource = m_tileSource.CompressTileSource(CompressionMethod.Basic, progressManager);

			return m_tileSource;
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
