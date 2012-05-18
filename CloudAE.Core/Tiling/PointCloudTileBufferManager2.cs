﻿using System;
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
	unsafe class PointCloudTileBufferManager2 : IPointCloudTileBufferManager, IPropertyContainer
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
			
			m_createdBuffers = new PointCloudTileBuffer[m_tileSet.Cols + 1, m_tileSet.Rows + 1];

			foreach (PointCloudTile tile in m_tileSet.ValidTiles)
			{
				m_createdBuffers[tile.Col, tile.Row] = new PointCloudTileBuffer(tile, this);
				m_createdBuffers[tile.Col, tile.Row].ActivateBuffer(new byte[tile.StorageSize]);
			}

			// buffer the edges for overflow
			for (int x = 0; x < m_tileSet.Cols; x++)
				m_createdBuffers[x, m_tileSet.Rows] = m_createdBuffers[x, m_tileSet.Rows - 1];
			for (int y = 0; y <= m_tileSet.Rows; y++)
				m_createdBuffers[m_tileSet.Cols, y] = m_createdBuffers[m_tileSet.Cols - 1, y];
		}

		public void AddPoint(byte* p, int tileX, int tileY)
		{
			m_createdBuffers[tileX, tileY].AddPoint(p);
		}

		public UQuantizedExtent3D FinalizeTiles(ProgressManager progressManager)
		{
			using (ProgressManagerProcess process = progressManager.StartProcess("FinalizeTiles"))
			{
				foreach (PointCloudTile tile in m_tileSet.ValidTiles)
				{
					PointCloudTileBuffer tileBuffer = m_createdBuffers[tile.Col, tile.Row];

					m_tileSet[tile.Col, tile.Row].QuantizedExtent = tileBuffer.GetExtent();

					tileBuffer.Flush(m_outputStream);
					tileBuffer.DeactivateBuffer();

					if (!process.Update(tile))
						break;
				}
			}

			UQuantizedExtent3D newQuantizedExtent = m_tileSet.ValidTiles.Select(t => t.QuantizedExtent).Union();
			return newQuantizedExtent;
		}
	}
}
