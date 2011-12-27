using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CloudAE.Core.Geometry
{
	/// <summary>
	/// Immutable point class.
	/// </summary>
	public struct Point3D
	{
		public readonly double X;
		public readonly double Y;
		public readonly double Z;

		public Point3D(double x, double y, double z)
		{
			X = x;
			Y = y;
			Z = z;
		}

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
