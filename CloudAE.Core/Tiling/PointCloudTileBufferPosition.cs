using System;
using System.Collections.Generic;
using System.Linq;

namespace CloudAE.Core
{
	public unsafe class PointCloudTileBufferPosition
	{
		private readonly short m_pointSizeBytes;

		public byte* DataPtr;
		public readonly byte* DataEndPtr;

		public PointCloudTileBufferPosition(PointBufferWrapper buffer, PointCloudTile tile)
		{
			m_pointSizeBytes = tile.TileSet.TileSource.PointSizeBytes;

			DataPtr = buffer.PointDataPtr + (tile.PointOffset * m_pointSizeBytes);
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

	public unsafe class GridBufferPosition
	{
		private readonly short m_entrySize;

		public byte* DataPtr;
		public readonly byte* DataEndPtr;

		public GridBufferPosition(PointBufferWrapper buffer, int index, int count, short entrySize)
		{
			m_entrySize = entrySize;

			DataPtr = buffer.PointDataPtr + (index * m_entrySize);
			DataEndPtr = DataPtr + (count * m_entrySize);
		}

		public void Swap(byte* pSource)
		{
			for (int i = 0; i < m_entrySize; i++)
			{
				byte temp = DataPtr[i];
				DataPtr[i] = pSource[i];
				pSource[i] = temp;
			}

			DataPtr += m_entrySize;
		}

		public void Increment()
		{
			DataPtr += m_entrySize;
		}
	}
}
