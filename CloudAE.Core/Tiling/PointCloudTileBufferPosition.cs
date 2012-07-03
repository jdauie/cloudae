using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CloudAE.Core.Geometry;

namespace CloudAE.Core
{
	unsafe class PointCloudTileBufferPosition
	{
		public readonly PointCloudTile Tile;

		private readonly BufferInstance m_buffer;

		public byte* DataPtr;
		public readonly byte* DataEndPtr;

		public PointCloudTileBufferPosition(BufferInstance buffer, PointCloudTile tile)
		{
			Tile = tile;
			m_buffer = buffer;

			DataPtr = m_buffer.DataPtr + (tile.PointOffset * tile.TileSource.PointSizeBytes);
			DataEndPtr = DataPtr + tile.PointCount * tile.TileSource.PointSizeBytes;
		}

		public void Swap(byte* pSource)
		{
			//UQuantizedPoint3D* pTarget = (UQuantizedPoint3D*)(m_pStart + m_byteIndexWithinTile);

			//UQuantizedPoint3D temp = *pTarget;
			//*pTarget = *source;
			//*source = temp;

			for (int i = 0; i < Tile.TileSource.PointSizeBytes; i++)
			{
				byte temp = DataPtr[i];
				DataPtr[i] = pSource[i];
				pSource[i] = temp;
			}

			DataPtr += Tile.TileSource.PointSizeBytes;
		}

		public void Increment()
		{
			DataPtr += Tile.TileSource.PointSizeBytes;
		}
	}
}
