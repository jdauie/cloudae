using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CloudAE.Core.Geometry
{
	public struct SQuantizedPoint3D : IComparable<SQuantizedPoint3D>, IQuantizedPoint3D
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
