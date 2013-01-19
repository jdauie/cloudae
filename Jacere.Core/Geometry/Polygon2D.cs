using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;

namespace Jacere.Core.Geometry
{
	public class Polygon2D : PolygonBase<Point>
	{
		public Polygon2D(IEnumerable<Point> points)
			: base(points)
		{
		}
	}
}
