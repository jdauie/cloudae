﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CloudAE.Core.Geometry
{
	/// <summary>
	/// Immutable point class.
	/// </summary>
	public struct Point3D : IPoint3D
	{
		private readonly double m_x;
		private readonly double m_y;
		private readonly double m_z;

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

		public Point3D(double x, double y, double z)
		{
			m_x = x;
			m_y = y;
			m_z = z;
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
