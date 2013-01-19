using System;
using System.Collections.Generic;
using System.Linq;

using Jacere.Core;
using Jacere.Data.PointCloud;

namespace CloudAE.Core
{
	public unsafe class PointCloudTileSourceEnumeratorChunk : IPointDataTileChunk
	{
		private readonly PointCloudTile m_tile;

		private readonly byte* m_dataPtr;
		private readonly byte* m_dataEndPtr;

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

		public int Index
		{
			get { return Tile.ValidIndex; }
		}

		public byte[] Data
		{
			get { return m_buffer.Data; }
		}

		public byte* PointDataPtr
		{
			get { return m_dataPtr; }
		}

		public byte* PointDataEndPtr
		{
			get { return m_dataEndPtr; }
		}

		public int Length
		{
			get { return (int)(m_dataEndPtr - m_dataPtr); }
		}

		public short PointSizeBytes
		{
			get { return m_pointSizeBytes; }
		}

		public int PointCount
		{
			get { return m_tile.PointCount; }
		}

		public IPointDataChunk CreateSegment(int pointCount)
		{
			return new PointCloudTileSourceEnumeratorChunk(this, pointCount);
		}

		#endregion

		public PointCloudTileSourceEnumeratorChunk(PointCloudTile tile, BufferInstance buffer)
		{
			m_tile = tile;

			m_buffer = buffer;
			m_pointSizeBytes = m_tile.TileSet.TileSource.PointSizeBytes;

			m_dataPtr = buffer.DataPtr;
			m_dataEndPtr = m_dataPtr + m_tile.StorageSize;
		}

		public PointCloudTileSourceEnumeratorChunk(PointCloudTileSourceEnumeratorChunk chunk, int pointCount)
			: this(chunk.m_tile, chunk.m_buffer)
		{
			if (pointCount > m_tile.PointCount)
				throw new Exception("Too many points");

			m_dataEndPtr = m_dataPtr + pointCount * m_pointSizeBytes;
		}
	}
}
