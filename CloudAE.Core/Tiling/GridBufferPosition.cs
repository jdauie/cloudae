using System;
using Jacere.Data.PointCloud;

namespace CloudAE.Core
{
	public unsafe class GridBufferPosition
	{
		private readonly short m_entrySize;

		public byte* DataPtr;
		public readonly byte* DataEndPtr;

		public bool IsIncomplete
		{
			get { return (DataEndPtr != DataPtr); }
		}

		public GridBufferPosition(IPointDataChunk buffer, int index, int count, short entrySize)
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
