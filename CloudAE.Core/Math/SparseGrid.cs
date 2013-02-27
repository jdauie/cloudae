using System;
using System.Collections.Generic;
using System.Linq;

using Jacere.Core.Geometry;

namespace CloudAE.Core
{
	public class SparseGrid<T> : IGrid
	{
		private readonly GridDefinition m_def;

		private readonly int m_bitsX;
		private readonly int m_bitsY;

		private readonly T[] m_data;
		private readonly int[] m_index;

		#region Properties

		public ushort SizeX
		{
			get { return m_def.SizeX; }
		}

		public ushort SizeY
		{
			get { return m_def.SizeY; }
		}

		#endregion

		private SparseGrid(ushort sizeX, ushort sizeY)
		{
			m_def = new GridDefinition(sizeX, sizeY);

			m_bitsX = GetBits(SizeX);
			m_bitsY = GetBits(SizeY);

			m_extent = extent;

			int edgeBufferSize = bufferEdge ? 1 : 0;

			Data = new T[SizeY + edgeBufferSize, SizeX + edgeBufferSize];

			if (!fillValIsDefault)
				Reset();
		}

		public void Reset()
		{
			T fillVal = FillVal;
			int sizeY = Data.GetLength(0);
			int sizeX = Data.GetLength(1);

			for (int y = 0; y < sizeY; y++)
				for (int x = 0; x < sizeX; x++)
					Data[y, x] = fillVal;
		}

		public int GetIndex()
		{
			return 0;
		}
	}
}
