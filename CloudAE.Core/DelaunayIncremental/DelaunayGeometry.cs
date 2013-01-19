using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Jacere.Core.Geometry;

namespace CloudAE.Core.DelaunayIncremental
{
	static class DelaunayGeometry
	{
		////use this only for direction and nothing else
		public static float CrossProduct(DelaunayPoint p, DelaunayPoint a, DelaunayPoint b)
		{
			// FUTURE: Migrate this into a more appropriate place.
			//use the multiplier for making sure we handle small numbers correctly
			double diffax = (double)a.X - p.X; 
			double diffay = (double)a.Y - p.Y;
			double diffbx = (double)b.X - p.X;
			double diffby = (double)b.Y - p.Y;

			double cross = (diffax * diffby - diffbx * diffay);

			if (cross >= 1.0e-12 || cross <= -1.0e-12)
				return (float)cross;

			return 0.0f;
		}

		public static Extent2D Circumcircle(DelaunayPoint v1, DelaunayPoint v2, DelaunayPoint v3, double cx, double cy)
		{
			const double ERROR_BOUND = 0.005;

			//http://mathworld.wolfram.com/Circumcircle.html
			double x1 = v1.X;
			double x2 = v2.X;
			double x3 = v3.X;
			double y1 = v1.Y;
			double y2 = v2.Y;
			double y3 = v3.Y;

			double x1Sq = x1 * x1;
			double x2Sq = x2 * x2;
			double x3Sq = x3 * x3;
			double y1Sq = y1 * y1;
			double y2Sq = y2 * y2;
			double y3Sq = y3 * y3;

			double a = (x1 - x2) * (y2 - y3) - (y1 - y2) * (x2 - x3);
			double bx = -((x1Sq + y1Sq - x2Sq - y2Sq) * (y2 - y3) - (x2Sq + y2Sq - x3Sq - y3Sq) * (y1 - y2));
			double by = ((x1Sq + y1Sq - x2Sq - y2Sq) * (x2 - x3) - (x2Sq + y2Sq - x3Sq - y3Sq) * (x1 - x2));
			double c = -((x1Sq + y1Sq) * (x2 * y3 - x3 * y2) - (x2Sq + y2Sq) * (x1 * y3 - x3 * y1) + (x3Sq + y3Sq) * (x1 * y2 - x2 * y1));

			Extent2D extent = null;

			if (a == 0.0f)
			{
				extent = new Extent2D(cx - ERROR_BOUND, cx + ERROR_BOUND, cy - ERROR_BOUND, cy + ERROR_BOUND);
			}
			else
			{
				double x0 = -bx / (2.0f * a);
				double y0 = -by / (2.0f * a);
				double numerator = bx * bx + by * by - 4.0f * a * c;

				double rad = 0;
				if (numerator >= 0)
					rad = Math.Sqrt(numerator) / (2.0f * Math.Abs(a));

				extent = new Extent2D(x0 - rad - ERROR_BOUND, x0 + rad + ERROR_BOUND, y0 - rad - ERROR_BOUND, y0 + rad + ERROR_BOUND);
			}
			return extent;
		}
	}
}
