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
	class PointCloudTileBufferManager2 : IPointCloudTileBufferManager, IPropertyContainer
	{
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

			foreach (PointCloudTile tile in m_tileSet.ValidTiles)
			{
				m_createdBuffers[tile.Col, tile.Row] = new PointCloudTileBuffer(tile, this);
				m_createdBuffers[tile.Col, tile.Row].ActivateBuffer(new byte[tile.StorageSize]);
			}
		}

		public void AddPoint(UQuantizedPoint3D point, int tileX, int tileY)
		{
			m_createdBuffers[tileX, tileY].AddPoint(point);
		}

		public UQuantizedExtent3D FinalizeTiles(ProgressManager progressManager)
		{
			foreach (PointCloudTile tile in m_tileSet)
			{
				UQuantizedExtent3D quantizedExtent = m_tileSet.ComputeTileExtent(tile, m_tileSource.QuantizedExtent);
				if (tile.IsValid)
					quantizedExtent = m_createdBuffers[tile.Col, tile.Row].GetExtent().Union2D(quantizedExtent);

				m_tileSet[tile.Col, tile.Row] = new PointCloudTile(tile, quantizedExtent);
			}

			foreach (PointCloudTile tile in m_tileSet.ValidTiles)
			{
				PointCloudTileBuffer tileBuffer = m_createdBuffers[tile.Col, tile.Row];
				
				tileBuffer.Flush(m_outputStream);
				tileBuffer.DeactivateBuffer();

				if (!progressManager.Update((float)tile.ValidIndex / m_tileSet.ValidTileCount))
					break;
			}

			List<UQuantizedExtent3D> debugExtents = new List<UQuantizedExtent3D>(m_tileSet.Select(t => t.QuantizedExtent));

			UQuantizedExtent3D newQuantizedExtent = m_tileSet.Select(t => t.QuantizedExtent).Union();
			return newQuantizedExtent;
		}
	}
}
