using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Diagnostics;

using Jacere.Core;
using Jacere.Core.Util;
using Jacere.Data.PointCloud;

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
					m_binarySource = m_inputHandler.GenerateBinarySource(progressManager);

					using (var segmentBuffer = BufferManager.AcquireBuffer(m_id, (int)PROPERTY_SEGMENT_SIZE.Value, true))
					{
						var tileManager = new PointCloudTileManager(m_binarySource);
						m_tileSource = tileManager.TilePointFileIndex(m_tiledHandler, segmentBuffer, progressManager);
					}

					GC.Collect();

#warning this was for xyz, but I have not yet re-implemented that anyway
					//if (m_binarySource.FilePath != m_inputHandler.FilePath)
					//    File.Delete(m_binarySource.FilePath);

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
	}
}
