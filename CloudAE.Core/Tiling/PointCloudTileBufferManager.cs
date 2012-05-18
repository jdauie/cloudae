using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using CloudAE.Core.Geometry;

namespace CloudAE.Core
{
	enum PointCloudTileBufferSizeMode : ushort
	{
		Median = 1,
		P1000
	}

	/// <summary>
	/// The performance penalty of this mechanism is due to the random write operations.
	/// Sequential should be much faster.
	/// However, I am keeping this code because it could be faster on a solid-state disk.
	/// </summary>
	[Obsolete]
	unsafe class PointCloudTileBufferManager : IPointCloudTileBufferManager, IPropertyContainer
	{
		private static readonly PropertyState<ByteSizesSmall> PROPERTY_MAX_BUFFER_SIZE;
		private static readonly PropertyState<PointCloudTileBufferSizeMode> PROPERTY_BUFFER_SIZE_MODE;

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

		static PointCloudTileBufferManager()
		{
			PROPERTY_MAX_BUFFER_SIZE = Context.RegisterOption<ByteSizesSmall>(Context.OptionCategory.Tiling, "BufferManagerMaxBufferSize", ByteSizesSmall.MB_512);
			PROPERTY_BUFFER_SIZE_MODE = Context.RegisterOption<PointCloudTileBufferSizeMode>(Context.OptionCategory.Tiling, "BufferManagerSizeMode", PointCloudTileBufferSizeMode.Median);
		}

		public PointCloudTileBufferManager(PointCloudTileSource tileSource, FileStream outputStream)
		{
			m_tileSource = tileSource;

			m_tileSet = m_tileSource.TileSet;
			m_outputStream = outputStream;

			m_activeBufferPriority = new LinkedList<PointCloudTileBuffer>();
			
			m_createdBuffers = new PointCloudTileBuffer[m_tileSet.Cols + 1, m_tileSet.Rows + 1];
			m_activeBuffers = new LinkedListNode<PointCloudTileBuffer>[m_tileSet.Cols, m_tileSet.Rows];

			foreach (PointCloudTile tile in m_tileSet.ValidTiles)
				m_createdBuffers[tile.Col, tile.Row] = new PointCloudTileBuffer(tile, this);

			// buffer the edges for overflow
			for (int x = 0; x < m_tileSet.Cols; x++)
				m_createdBuffers[x, m_tileSet.Rows] = m_createdBuffers[x, m_tileSet.Rows - 1];
			for (int y = 0; y <= m_tileSet.Rows; y++)
				m_createdBuffers[m_tileSet.Cols, y] = m_createdBuffers[m_tileSet.Cols - 1, y];
			
			// allocate buffers
			int maxIndividualBufferSize = m_tileSource.PointSizeBytes;

			switch (PROPERTY_BUFFER_SIZE_MODE.Value)
			{
				case PointCloudTileBufferSizeMode.P1000:
					maxIndividualBufferSize *= 1000;
					break;
				case PointCloudTileBufferSizeMode.Median:
				default:
					maxIndividualBufferSize *= m_tileSet.Density.MedianTileCount;
					break;
			}

			int maxBuffersToAllocate = Math.Min((int)PROPERTY_MAX_BUFFER_SIZE.Value / maxIndividualBufferSize, m_tileSet.TileCount);

			Context.WriteLine("Tiles:   {0}", m_tileSet.TileCount);
			Context.WriteLine("Buffers: {0}", maxBuffersToAllocate);

			if (maxBuffersToAllocate == 0)
				throw new Exception("One of the tiles is huge");

			m_dataBuffers = new LinkedList<byte[]>();
			for (int i = 0; i < maxBuffersToAllocate; i++)
				m_dataBuffers.AddLast(new byte[maxIndividualBufferSize]);
		}

		public void AddPoint(byte* p, int tileX, int tileY)
		{
			PointCloudTileBuffer tileBuffer = m_createdBuffers[tileX, tileY];

			ActivateBuffer(tileBuffer);

			if (tileBuffer.AddPoint(p))
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
			foreach (PointCloudTile tile in m_tileSet.ValidTiles)
				m_tileSet[tile.Col, tile.Row].QuantizedExtent = m_createdBuffers[tile.Col, tile.Row].GetExtent();

			UQuantizedExtent3D newQuantizedExtent = m_tileSet.ValidTiles.Select(t => t.QuantizedExtent).Union();
			return newQuantizedExtent;
		}
	}
}
