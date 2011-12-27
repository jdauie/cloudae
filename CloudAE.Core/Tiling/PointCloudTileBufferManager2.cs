using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using CloudAE.Core.Geometry;

namespace CloudAE.Core
{
	/// <summary>
	/// This is intended to test sequential output by not flushing until the end.
	/// </summary>
	class PointCloudTileBufferManager2 : IPointCloudTileBufferManager
	{
		/// <summary>
		/// This has a substantial performance impact.
		/// The choice of individual buffer size has a larger impact than max buffer size (above a reasonable size).
		/// </summary>
		private const int MAX_BUFFER_SIZE_BYTES = 1 << 29; // 29->512MB

		private readonly PointCloudTileSource m_tileSource;

		private PointCloudTileSet m_tileSet;
		private FileStream m_outputStream;

		private PointCloudTileBuffer[,] m_createdBuffers;

		public PointCloudTileSource TileSource
		{
			get { return m_tileSource; }
		}

		public PointCloudTileBufferManager2(PointCloudTileSource tileSource, FileStream outputStream)
		{
			m_tileSource = tileSource;

			m_tileSet = m_tileSource.TileSet;
			m_outputStream = outputStream;
			
			m_createdBuffers = new PointCloudTileBuffer[m_tileSet.Cols, m_tileSet.Rows];

			for (int x = 0; x < m_tileSet.Cols; x++)
			{
				for (int y = 0; y < m_tileSet.Rows; y++)
				{
					PointCloudTile tile = m_tileSet.Tiles[x, y];
					if (tile.IsValid)
					{
						m_createdBuffers[x, y] = new PointCloudTileBuffer(tile, this);
						m_createdBuffers[x, y].ActivateBuffer(new byte[tile.StorageSize]);
					}
				}
			}
		}

		public void AddPoint(UQuantizedPoint3D point, int tileX, int tileY)
		{
			m_createdBuffers[tileX, tileY].AddPoint(point);
		}

		public UQuantizedExtent3D FinalizeTiles(ProgressManager progressManager)
		{
			for (int x = 0; x < m_tileSet.Cols; x++)
			{
				for (int y = 0; y < m_tileSet.Rows; y++)
				{
					PointCloudTile tile = m_tileSet.Tiles[x, y];
					UQuantizedExtent3D quantizedExtent = m_tileSet.ComputeTileExtent(tile, m_tileSource.QuantizedExtent);
					if (tile.PointCount > 0)
						quantizedExtent = m_createdBuffers[x, y].GetExtent().Union2D(quantizedExtent);

					m_tileSet.Tiles[x, y] = new PointCloudTile(tile, quantizedExtent);
				}
			}

			int validTileIndex = 0;
			foreach (PointCloudTile tile in m_tileSet)
			{
				PointCloudTileBuffer tileBuffer = m_createdBuffers[tile.Col, tile.Row];
				if (tileBuffer != null)
				{
					tileBuffer.Flush(m_outputStream);
					tileBuffer.DeactivateBuffer();
				}

				if (!progressManager.Update((float)validTileIndex / m_tileSet.ValidTileCount))
					break;

				++validTileIndex;
			}
			progressManager.Update(1);

			List<UQuantizedExtent3D> debugExtents = new List<UQuantizedExtent3D>(m_tileSet.Select(t => t.QuantizedExtent));

			UQuantizedExtent3D newQuantizedExtent = m_tileSet.Select(t => t.QuantizedExtent).Union();
			return newQuantizedExtent;
		}
	}
}
