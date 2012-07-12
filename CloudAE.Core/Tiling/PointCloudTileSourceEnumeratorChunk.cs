using System;
using System.Collections.Generic;
using System.Linq;

namespace CloudAE.Core
{
	public unsafe class PointCloudTileSourceEnumeratorChunk : IProgress, IPointDataTileChunk
	{
		public readonly PointCloudTile m_tile;

		public readonly byte* DataPtr;
		public readonly byte* DataEndPtr;

		private readonly BufferInstance m_buffer;
		private readonly short m_pointSizeBytes;

		public float Progress
		{
			get { return Tile.Progress; }
		}

		public PointCloudTile Tile
		{
			get { return m_tile; }
		}

		#region IPointDataChunk Members

		byte[] IPointDataChunk.Data
		{
			get { return m_buffer.Data; }
		}

		public byte* PointDataPtr
		{
			get { return DataPtr; }
		}

		public byte* PointDataEndPtr
		{
			get { return DataEndPtr; }
		}

		public int Length
		{
			get { return m_tile.StorageSize; }
		}

		public short PointSizeBytes
		{
			get { return m_pointSizeBytes; }
		}

		public int PointCount
		{
			get { return m_tile.PointCount; }
		}

		#endregion

		public PointCloudTileSourceEnumeratorChunk(PointCloudTile tile, BufferInstance buffer)
		{
			m_tile = tile;

			m_buffer = buffer;
			m_pointSizeBytes = m_tile.TileSet.TileSource.PointSizeBytes;

			DataPtr = buffer.DataPtr;
			DataEndPtr = DataPtr + m_tile.StorageSize;
		}
	}
}
