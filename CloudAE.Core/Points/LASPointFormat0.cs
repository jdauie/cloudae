using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;

namespace CloudAE.Core.Geometry
{
	[StructLayout(LayoutKind.Sequential)]
	public struct LASPointFormat0 : IQuantizedPoint3D
	{
		public int X;
		public int Y;
		public int Z;
		public ushort Intensity;
		public byte Options;
		public byte Classifications;
		public sbyte ScanAngleRank;
		public byte UserData;
		public ushort PointSourceID;

		//public LASPointFormat0(int x, int y, int z)
		//{
		//    X = x;
		//    Y = y;
		//    Z = z;
		//}

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
