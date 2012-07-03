using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CloudAE.Core.Geometry;

namespace CloudAE.Core
{
	unsafe class PointCloudTileBufferPosition
	{
		private readonly short m_pointSizeBytes;

		public byte* DataPtr;
		public readonly byte* DataEndPtr;

		public PointCloudTileBufferPosition(BufferInstance buffer, PointCloudTile tile)
		{
			m_pointSizeBytes = tile.TileSource.PointSizeBytes;

			DataPtr = buffer.DataPtr + (tile.PointOffset * m_pointSizeBytes);
			DataEndPtr = DataPtr + tile.PointCount * m_pointSizeBytes;
		}

		public void Swap(byte* pSource)
		{
			for (int i = 0; i < m_pointSizeBytes; i++)
			{
				byte temp = DataPtr[i];
				DataPtr[i] = pSource[i];
				pSource[i] = temp;
			}

			DataPtr += m_pointSizeBytes;
		}

		public void Increment()
		{
			DataPtr += m_pointSizeBytes;
		}
	}
}
