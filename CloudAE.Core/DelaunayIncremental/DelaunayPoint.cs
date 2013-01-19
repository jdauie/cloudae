using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Jacere.Core.Geometry;

namespace CloudAE.Core.DelaunayIncremental
{
	public class DelaunayPoint : IPoint3D
	{
		public readonly float m_x;
		public readonly float m_y;
		public readonly float m_z;

		public int Index;

		public double X
		{
			get { return m_x; }
		}

		public double Y
		{
			get { return m_y; }
		}

		public double Z
		{
			get { return m_z; }
		}

		public DelaunayPoint(double x, double y, double z, int index)
		{
			m_x = (float)x;
			m_y = (float)y;
			m_z = (float)z;
			Index = index;
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
