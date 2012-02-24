using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;

namespace CloudAE.Core.Geometry
{
	[StructLayout(LayoutKind.Sequential)]
	public struct UQuantizedPoint3D : IComparable<UQuantizedPoint3D>, IQuantizedPoint3D
	{
		public uint X;
		public uint Y;
		public uint Z;

		public UQuantizedPoint3D(uint x, uint y, uint z)
		{
			X = x;
			Y = y;
			Z = z;
		}

		public int CompareTo(UQuantizedPoint3D other)
		{
			int cmp = X.CompareTo(other.X);
			if (cmp == 0)
			{
				cmp = Y.CompareTo(other.Y);
				if (cmp == 0)
					cmp = Z.CompareTo(other.Z);
			}
			return cmp;
		}

		/// <summary>
		/// Returns a <see cref="System.String"/> that represents this instance.
		/// </summary>
		/// <returns>
		/// A <see cref="System.String"/> that represents this instance.
		/// </returns>
		public override string ToString()
		{
			return String.Format("({0}, {1}, {2})", X, Y, Z);
		}
	}

	public class UQuantizedPoint3DGridComparer : IComparer<UQuantizedPoint3D>
	{
		private readonly int m_gridSize;
		private readonly int m_gridCellDimensionX;
		private readonly int m_gridCellDimensionY;

		public UQuantizedPoint3DGridComparer(int gridSize, int gridCellDimensionX, int gridCellDimensionY)
		{
			m_gridSize = gridSize;

			m_gridCellDimensionX = gridCellDimensionX;
			m_gridCellDimensionY = gridCellDimensionY;
		}

		public int Compare(UQuantizedPoint3D a, UQuantizedPoint3D b)
		{
			int xBinA = (int)(a.X / m_gridCellDimensionX);
			int xBinB = (int)(b.X / m_gridCellDimensionX);

			if (xBinA == m_gridSize) --xBinA;
			if (xBinB == m_gridSize) --xBinB;

			int diff = xBinA - xBinB;
			if (diff == 0)
			{
				int yBinA = (int)(a.Y / m_gridCellDimensionY);
				int yBinB = (int)(b.Y / m_gridCellDimensionY);

				if (yBinA == m_gridSize) --yBinA;
				if (yBinB == m_gridSize) --yBinB;

				diff = yBinA - yBinB;
				if (diff == 0)
				{
					diff = a.Z.CompareTo(b.Z);
				}
			}
			return diff;
		}
	}
}
