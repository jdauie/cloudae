using System;
using System.Collections.Generic;
using System.Linq;

using Jacere.Core.Geometry;

namespace CloudAE.Core
{
	public class GridDefinition : IGrid
	{
		private readonly ushort m_sizeX;
		private readonly ushort m_sizeY;

		private readonly int m_bitsX;
		private readonly int m_bitsY;

		#region Properties

		public ushort SizeX
		{
			get { return m_sizeX; }
		}

		public ushort SizeY
		{
			get { return m_sizeY; }
		}

		public int Size
		{
			get { return SizeX * SizeY; }
		}

		#endregion

		#region Creators

		public static GridDefinition Create(IAspect extent, ushort minDimension, ushort maxDimension)
		{
			ushort sizeX = maxDimension;
			ushort sizeY = maxDimension;

			double aspect = extent.Aspect;
			if (aspect > 1)
				sizeY = (ushort)Math.Max(sizeX / aspect, minDimension);
			else
				sizeX = (ushort)Math.Max(sizeX * aspect, minDimension);

			return new GridDefinition(sizeX, sizeY);
		}

		#endregion

		public GridDefinition(ushort sizeX, ushort sizeY)
		{
			m_sizeX = sizeX;
			m_sizeY = sizeY;

#warning THIS DOES NOT ACCOUNT FOR BUFFERED EDGES
			m_bitsX = GetBits(m_sizeX);
			m_bitsY = GetBits(m_sizeY);

			//int inflatedCount = (1 << (m_bitsX + m_bitsY));
			//int inflatedMax = inflatedCount - 1;
		}

		public int GetIndex(ushort x, ushort y)
		{
			return ((y << m_bitsX) | x);
		}

		public int GetIndex(int incrementalIndex)
		{
			int y = (incrementalIndex / m_sizeX);
			int x = (incrementalIndex % m_sizeX);
			return ((y << m_bitsX) | x);
		}

		private static int GetBits(ushort val)
		{
			return (int)Math.Ceiling(Math.Log(val, 2));
		}
	}
}
