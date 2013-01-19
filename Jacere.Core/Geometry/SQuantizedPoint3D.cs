using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace Jacere.Core.Geometry
{
	[StructLayout(LayoutKind.Sequential)]
	public struct SQuantizedPoint3D : IComparable<SQuantizedPoint3D>, IQuantizedPoint3D, ISerializeBinary
	{
		public int X;
		public int Y;
		public int Z;

		public SQuantizedPoint3D(int x, int y, int z)
		{
			X = x;
			Y = y;
			Z = z;
		}

		public SQuantizedPoint3D(Point3D point)
		{
			X = (int)point.X;
			Y = (int)point.Y;
			Z = (int)point.Z;
		}

		public SQuantizedPoint3D(BinaryReader reader)
		{
			X = reader.ReadInt32();
			Y = reader.ReadInt32();
			Z = reader.ReadInt32();
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

		public int CompareTo(SQuantizedPoint3D other)
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
}
