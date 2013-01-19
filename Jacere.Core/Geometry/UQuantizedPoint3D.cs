﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace Jacere.Core.Geometry
{
	[StructLayout(LayoutKind.Sequential)]
	public struct UQuantizedPoint3D : IComparable<UQuantizedPoint3D>, IQuantizedPoint3D, ISerializeBinary
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

		public UQuantizedPoint3D(Point3D point)
		{
			X = (uint)point.X;
			Y = (uint)point.Y;
			Z = (uint)point.Z;
		}

		public UQuantizedPoint3D(BinaryReader reader)
		{
			X = reader.ReadUInt32();
			Y = reader.ReadUInt32();
			Z = reader.ReadUInt32();
		}

		public void Serialize(BinaryWriter writer)
		{
			writer.Write(X);
			writer.Write(Y);
			writer.Write(Z);
		}

		public Point3D GetPoint3D()
		{
			return new Point3D(X, Y, Z);
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