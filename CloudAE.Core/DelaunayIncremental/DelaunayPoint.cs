using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CloudAE.Core.Geometry;

namespace CloudAE.Core.DelaunayIncremental
{
	public class DelaunayPoint : IPoint3D
	{
		public readonly float X;
		public readonly float Y;
		public readonly float Z;

		public int Index;

		public DelaunayPoint(double x, double y, double z, int index)
		{
			X = (float)x;
			Y = (float)y;
			Z = (float)z;
			Index = index;
		}

		public double GetX() { return X; }
		public double GetY() { return Y; }
		public double GetZ() { return Z; }

		/// <summary>
		/// Returns a <see cref="System.String"/> that represents this instance.
		/// </summary>
		/// <returns>
		/// A <see cref="System.String"/> that represents this instance.
		/// </returns>
		public override string ToString()
		{
			return String.Format("({0:f}, {1:f}, {2:f})", X, Y, Z);
		}
	}
}
