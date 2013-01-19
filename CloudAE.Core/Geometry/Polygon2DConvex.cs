using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;

namespace CloudAE.Core.Geometry
{
	public class Polygon2DConvex : Polygon2D
	{
		public Polygon2DConvex(IEnumerable<Point> points)
			: base(points)
		{
			if (!IsOutlineConvex())
				throw new ArgumentException("Polygon is not convex", "points");
		}

		private bool IsOutlineConvex()
		{
			if (IsDegenerate)
				return false;

			int xChanges = 0;
			int yChanges = 0;

			Vector a = m_points[m_points.Length - 1] - m_points[0];
			for (int i = 0; i < m_points.Length - 1; i++)
			{
				Vector b = m_points[i] - m_points[i + 1];

				if ((a.X < 0) != (b.X < 0)) ++xChanges;
				if ((a.Y < 0) != (b.Y < 0)) ++yChanges;

				a = b;
			}

			return (xChanges <= 2 && yChanges <= 2);
		}
	}
}
