using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CloudAE.Core
{
	unsafe class PointCloudTileBufferPosition
	{
		public int ByteIndex;

		public readonly PointCloudTile Tile;

		private readonly int m_startPointIndex;
		private readonly int m_startByteIndex;

		public int PointLength
		{
			get { return ByteLength / Tile.TileSource.PointSizeBytes; }
		}

		public int ByteLength
		{
			get { return (int)(ByteIndex - m_startByteIndex); }
		}

		public PointCloudTileBufferPosition(PointCloudTile tile)
		{
			Tile = tile;
			m_startPointIndex = tile.PointOffset;
			m_startByteIndex = m_startPointIndex * tile.TileSource.PointSizeBytes;

			ByteIndex = m_startByteIndex;
		}
	}
}
