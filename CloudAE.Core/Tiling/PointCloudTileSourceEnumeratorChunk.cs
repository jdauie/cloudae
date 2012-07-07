using System;
using System.Collections.Generic;
using System.Linq;

namespace CloudAE.Core
{
	public unsafe class PointCloudTileSourceEnumeratorChunk : IProgress, IPointDataChunk
	{
		public readonly PointCloudTile Tile;

		public readonly byte* DataPtr;
		public readonly byte* DataEndPtr;

		private readonly BufferInstance m_buffer;
		private readonly short m_pointSizeBytes;

		public float Progress
		{
			get { return Tile.Progress; }
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
			get { return Tile.StorageSize; }
		}

		public short PointSizeBytes
		{
			get { return m_pointSizeBytes; }
		}

		public int PointCount
		{
			get { return Tile.PointCount; }
		}

		#endregion

		public PointCloudTileSourceEnumeratorChunk(PointCloudTile tile, BufferInstance buffer)
		{
			Tile = tile;

			m_buffer = buffer;
			m_pointSizeBytes = Tile.TileSource.PointSizeBytes;

			DataPtr = buffer.DataPtr;
			DataEndPtr = DataPtr + Tile.StorageSize;
		}
	}
}
