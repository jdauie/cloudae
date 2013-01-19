using System;

namespace Jacere.Core.Geometry
{
	/// <summary>
	/// Immutable point class.
	/// </summary>
	public struct Point2D : IPoint2D
	{
		private readonly double m_x;
		private readonly double m_y;

		public double X
		{
			get { return m_x; }
		}

		public double Y
		{
			get { return m_y; }
		}

		public Point2D(double x, double y)
		{
			m_x = x;
			m_y = y;
		}

		/// <summary>
		/// Returns a <see cref="System.String"/> that represents this instance.
		/// </summary>
		/// <returns>
		/// A <see cref="System.String"/> that represents this instance.
		/// </returns>
		public override string ToString()
		{
			return String.Format("({0:f}, {1:f})", X, Y);
		}
	}
}
