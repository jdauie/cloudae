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
}
