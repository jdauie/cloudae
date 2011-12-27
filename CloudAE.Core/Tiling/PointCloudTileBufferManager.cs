using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using CloudAE.Core.Geometry;

namespace CloudAE.Core
{
	/// <summary>
	/// The performance penalty of this mechanism is due to the random write operations.
	/// Sequential should be much faster.
	/// </summary>
	class PointCloudTileBufferManager : IPointCloudTileBufferManager
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
		private LinkedListNode<PointCloudTileBuffer>[,] m_activeBuffers;
		private LinkedList<PointCloudTileBuffer> m_activeBufferPriority;

		private LinkedList<byte[]> m_dataBuffers;

		public PointCloudTileSource TileSource
		{
			get { return m_tileSource; }
		}

		public PointCloudTileBufferManager(PointCloudTileSource tileSource, FileStream outputStream)
		{
			m_tileSource = tileSource;

			m_tileSet = m_tileSource.TileSet;
			m_outputStream = outputStream;

			m_activeBufferPriority = new LinkedList<PointCloudTileBuffer>();
			
			m_createdBuffers = new PointCloudTileBuffer[m_tileSet.Cols, m_tileSet.Rows];
			m_activeBuffers = new LinkedListNode<PointCloudTileBuffer>[m_tileSet.Cols, m_tileSet.Rows];

			for (int x = 0; x < m_tileSet.Cols; x++)
				for (int y = 0; y < m_tileSet.Rows; y++)
					m_createdBuffers[x, y] = new PointCloudTileBuffer(m_tileSet.Tiles[x, y], this);
			
			// allocate buffers
			int maxIndividualBufferSize = m_tileSet.Density.MedianTileCount * m_tileSource.PointSizeBytes;
			//int maxIndividualBufferSize = 1000 * TileSource.PointSizeBytes;
			int maxBuffersToAllocate = Math.Min(MAX_BUFFER_SIZE_BYTES / maxIndividualBufferSize, m_tileSet.TileCount);

			Console.WriteLine(String.Format("Tiles:   {0}", m_tileSet.TileCount));
			Console.WriteLine(String.Format("Buffers: {0}", maxBuffersToAllocate));

			if (maxBuffersToAllocate == 0)
				throw new Exception("One of the tiles is huge");

			m_dataBuffers = new LinkedList<byte[]>();
			for (int i = 0; i < maxBuffersToAllocate; i++)
				m_dataBuffers.AddLast(new byte[maxIndividualBufferSize]);
		}

		public void AddPoint(UQuantizedPoint3D point, int tileX, int tileY)
		{
			// figure out how to optimize this method

			//PointCloudTile tile = m_tileSet.Tiles[tileX, tileY];
			PointCloudTileBuffer tileBuffer = m_createdBuffers[tileX, tileY];

			ActivateBuffer(tileBuffer);

			if (tileBuffer.AddPoint(point))
			{
				// buffer is full or tile is complete
				FlushTileBuffer(tileBuffer);
			}
		}

		private void ActivateBuffer(PointCloudTileBuffer tileBuffer)
		{
			LinkedListNode<PointCloudTileBuffer> tileBufferPriorityNode = m_activeBuffers[tileBuffer.Col, tileBuffer.Row];
			if (tileBufferPriorityNode == null)
			{
				if (m_dataBuffers.Count == 0)
				{
					// flush the LRU tile to free a buffer
					PointCloudTileBuffer lastTileBuffer = m_activeBufferPriority.Last.Value;
					FlushTileBuffer(lastTileBuffer);
				}

				// acquire a buffer
				byte[] buffer = m_dataBuffers.First.Value;
				m_dataBuffers.RemoveFirst();

				tileBuffer.ActivateBuffer(buffer);

				// add to priority queue
				tileBufferPriorityNode = m_activeBufferPriority.AddFirst(tileBuffer);
				m_activeBuffers[tileBuffer.Col, tileBuffer.Row] = tileBufferPriorityNode;
			}
			else if(tileBufferPriorityNode != m_activeBufferPriority.First)
			{
				// move to top of priority queue
				m_activeBufferPriority.Remove(tileBufferPriorityNode);
				m_activeBufferPriority.AddFirst(tileBufferPriorityNode);
			}
		}

		private void DeactivateBuffer(PointCloudTileBuffer tileBuffer)
		{
			byte[] buffer = tileBuffer.DeactivateBuffer();
			m_dataBuffers.AddLast(buffer);

			m_activeBufferPriority.Remove(m_activeBuffers[tileBuffer.Col, tileBuffer.Row]);
			m_activeBuffers[tileBuffer.Col, tileBuffer.Row] = null;
		}

		private unsafe void FlushTileBuffer(PointCloudTileBuffer tileBuffer)
		{
			tileBuffer.Flush(m_outputStream);
			DeactivateBuffer(tileBuffer);
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

			UQuantizedExtent3D newQuantizedExtent = m_tileSet.Select(t => t.QuantizedExtent).Union();
			return newQuantizedExtent;
		}
	}
}
