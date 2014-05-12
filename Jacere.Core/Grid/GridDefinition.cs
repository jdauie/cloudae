using System;
using System.Collections.Generic;
using System.Linq;

using Jacere.Core.Geometry;

namespace Jacere.Core
{
	public class GridDefinition : IGrid
	{
		private readonly ushort m_sizeX;
		private readonly ushort m_sizeY;

		private readonly ushort m_underlyingSizeX;
		private readonly ushort m_underlyingSizeY;

		private readonly int m_bitsX;
		private readonly int m_bitsY;

		public static int GetSimpleIndex(ushort y, ushort x)
		{
			return ((y << 16) | x);
		}

		#region Properties

		public ushort SizeX
		{
			get { return m_sizeX; }
		}

		public ushort SizeY
		{
			get { return m_sizeY; }
		}

		public ushort UnderlyingSizeX
		{
			get { return m_underlyingSizeX; }
		}

		public ushort UnderlyingSizeY
		{
			get { return m_underlyingSizeY; }
		}

		public int Size
		{
			get { return SizeX * SizeY; }
		}

		public int IndexSize
		{
			get { return (1 << (m_bitsX + m_bitsY)); }
		}

		//public bool Buffered
		//{
		//    get { return (SizeX != UnderlyingSizeX); }
		//}

		#endregion

		public GridDefinition(ushort sizeX, ushort sizeY, bool bufferEdge)
		{
			m_sizeX = sizeX;
			m_sizeY = sizeY;

			m_underlyingSizeX = m_sizeX;
			m_underlyingSizeY = m_sizeY;

			if (bufferEdge)
			{
				++m_underlyingSizeX;
				++m_underlyingSizeY;
			}

			m_bitsX = GetBits(m_underlyingSizeX);
			m_bitsY = GetBits(m_underlyingSizeY);
		}

		//public int GetIndex(ushort y, ushort x)
		public int GetIndex(int y, int x)
		{
			return ((y << m_bitsX) | x);
		}

		public int GetIndex(int incrementalIndex)
		{
			int y = (incrementalIndex / m_sizeX);
			int x = (incrementalIndex % m_sizeX);
			return ((y << m_bitsX) | x);
		}

		public IEnumerable<SimpleGridCoord> GetTileOrdering()
		{
			for (ushort y = 0; y < m_sizeY; y++)
				for (ushort x = 0; x < m_sizeX; x++)
					yield return new SimpleGridCoord(y, x);
		}

		private static int GetBits(ushort val)
		{
			return (int)Math.Ceiling(Math.Log(val, 2));
		}
	}
}
