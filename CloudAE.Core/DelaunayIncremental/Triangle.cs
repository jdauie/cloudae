using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Jacere.Core.Geometry;

namespace CloudAE.Core.DelaunayIncremental
{
	class Triangle
	{
		const double VERY_SMALL_DIST = 1.0e-5;
		const double MULTIPLIER = 1024;


		public Triangle[] adjacentFace; //the adjacent faces
		
		public bool visited; //for traversing in dfs order
		public bool remove; //if a Triangle_t has been removed

		float centroidX;
		float centroidY;

		public DelaunayPoint[] m_v;// The three corners of the Triangle, in CW order.

		Extent2D m_circumCircleExtent;
		float m_a;
		float m_b;
		float m_c;
		float m_d;
		float m_norm;
		Extent2D m_extent;

		public Triangle(DelaunayPoint v1, DelaunayPoint v2, DelaunayPoint v3)
		{
			m_v = new DelaunayPoint[] { v1, v2, v3 };

			ForceClockwise();

			adjacentFace = new Triangle[3];

			centroidX = (float)(v1.X + v2.X + v3.X) / 3.0f;
			centroidY = (float)(v1.Y + v2.Y + v3.Y) / 3.0f;

			m_extent = new Extent2D(m_v[0], m_v[1], m_v[2]);
		}

		private void ForceClockwise()
		{
 			if(DelaunayGeometry.CrossProduct(m_v[0], m_v[1], m_v[2]) >= 0)
			{
				// a positive value indicates counter clockwise
				// Swapping the first 2 points should be enough
				DelaunayPoint tmp = m_v[0];
				m_v[0] = m_v[1];
				m_v[1] = tmp;
			}
		}

		public Extent2D CircumCircleExtent()
		{
			if (m_circumCircleExtent == null)
				m_circumCircleExtent = DelaunayGeometry.Circumcircle(m_v[0], m_v[1], m_v[2], centroidX, centroidY);

			return m_circumCircleExtent;
		}

		Triangle GetAdjacent(DelaunayPoint start, DelaunayPoint end)
		{
			double t = 0;
			double s = SegmentsIntersect(m_v[0], m_v[1], start, end, t);

			if(t >= 0 && t <= 1 && s >= 0 && s <= 1) 
			{
				return adjacentFace[0];
			}

			s = SegmentsIntersect(m_v[1], m_v[2], start, end, t);

			if(t >= 0 && t <= 1 && s >= 0 && s <= 1) 
			{
				return adjacentFace[1];
			}

			s = SegmentsIntersect(m_v[2], m_v[0], start, end, t);

			if(t >= 0 && t <= 1 && s >= 0 && s <= 1)
			{
				return adjacentFace[2];
			}

			return null;

		}

		public bool Contains(DelaunayPoint p)
		{
			// Only bother to check if the point is within the initial extents of the Triangle_t
			if (m_extent.Contains(p.X, p.Y, true))
			{
				return (
					(DelaunayGeometry.CrossProduct(m_v[0], p, m_v[1]) >= 0.0) && 
					(DelaunayGeometry.CrossProduct(m_v[1], p, m_v[2]) >= 0.0) &&
					(DelaunayGeometry.CrossProduct(m_v[2], p, m_v[0]) >= 0.0));
			}

			return false;
		}


		// Returns true if the point is contained in the triangle otherwise returns false
		// if false then ref is the next triangle
		public bool Contains(DelaunayPoint p, ref Triangle next)
		{
			//unrolled the dot product logic for optimization
			// a refers to point m_v[0]
			// b refers to point m_v[1]
			// c refers to point m_v[1]
			// p is the point 
			double diffax = (m_v[0].X - p.X);
			double diffay = (m_v[0].Y - p.Y);
			double diffcx = (m_v[2].X - p.X);
			double diffcy = (m_v[2].Y - p.Y);

			double aXc = (diffax * diffcy - diffcx * diffay);

			if(aXc < 0.0) // if negative point is outside of edge a,c
			{
				next = adjacentFace[2];
				return false;
			}
			else
			{
				double diffbx = (m_v[1].X - p.X);
				double diffby = (m_v[1].Y - p.Y);

				double cXb = (diffcx * diffby - diffbx * diffcy);


				if(cXb < 0.0) // if 
				{
					next = adjacentFace[1];
					return false;
				}
				else
				{
					double bXa = (diffbx * diffay - diffax * diffby);
					if(bXa < 0.0)
					{
						next = adjacentFace[0];
						return false;
					}
				}
			}

			return true;
		}

		float SegmentsIntersect(DelaunayPoint a, DelaunayPoint b, DelaunayPoint c, DelaunayPoint d, double t)
		{
			//what if denominator is zero??
			double diff0 = (c.Y - a.Y) * MULTIPLIER;
			double diff1 = (d.X - c.X) * MULTIPLIER;
			double diff2 = (a.X - c.X) * MULTIPLIER;
			double diff3 = (d.Y - c.Y) * MULTIPLIER;
			double diff4 = (b.Y - a.Y) * MULTIPLIER;
			double diff5 = (b.X - a.X) * MULTIPLIER;

			t = (diff0 * diff1 + diff2 * diff3) / (diff4 * diff1 - diff5 * diff3);
			double s = (-diff2 * diff4 - diff0 * diff5) /(diff3 * diff5 - diff1 * diff4);

			if(Math.Abs(t) <= VERY_SMALL_DIST)
				t = 0;

			if(Math.Abs(s) <= VERY_SMALL_DIST)
				s = 0;

			return (float)s;
		}

		public bool CircumCircleContains(DelaunayPoint p)
		{
			// http://www.cs.cmu.edu/~quake/robust.html
			double a1 = (double)m_v[0].X - p.X;
			double a2 = (double)m_v[0].Y - p.Y;
			double a3 = a1*a1 + a2*a2;

			double b1 = (double)m_v[1].X - p.X;
			double b2 = (double)m_v[1].Y - p.Y;
			double b3 = b1*b1 + b2*b2;

			double c1 = (double)m_v[2].X - p.X;
			double c2 = (double)m_v[2].Y - p.Y;
			double c3 = c1*c1 + c2*c2;

			double det = a1 * (b2 * c3 - b3 * c2) - a2 * (b1 * c3 - b3 * c1) + a3 * (b1 * c2 - b2 * c1);

			return !(det >= double.Epsilon);
		}

		bool IsClose(DelaunayPoint p, double maxDistance)
		{
			if (m_norm == 0)
				ComputePlaneParams();

			double dist = Math.Abs((m_a * p.X + m_b * p.Y + m_c * p.Z + m_d) / m_norm);
			return (dist <= maxDistance);
		}

		private void ComputePlaneParams()
		{
			double x1 = m_v[0].X;
			double y1 = m_v[0].Y;
			double z1 = m_v[0].Z;

			double x2 = m_v[1].X;
			double y2 = m_v[1].Y;
			double z2 = m_v[1].Z;

			double x3 = m_v[2].X;
			double y3 = m_v[2].Y;
			double z3 = m_v[2].Z;

			m_a = (float)(y1 * (z2 - z3) + y2 * (z3 - z1) + y3 * (z1 - z2));
			m_b = (float)(z1 * (x2 - x3) + z2 * (x3 - x1) + z3 * (x1 - x2));
			m_c = (float)(x1 * (y2 - y3) + x2 * (y3 - y1) + x3 * (y1 - y2));
			m_d = (float)(-(x1 * (y2 * z3 - y3 * z2) + x2 * (y3 * z1 - y1 * z3) + x3 * (y1 * z2 - y2 * z1)));
			m_norm = (float)Math.Sqrt(m_a * m_a + m_b * m_b + m_c * m_c);
		}

		public bool IsCoincidentWithVertex(DelaunayPoint p)
		{
			double distA = (m_v[0].X - p.X) * (m_v[0].X - p.X) + (m_v[0].Y - p.Y) * (m_v[0].Y - p.Y);
			double distB = (m_v[1].X - p.X) * (m_v[1].X - p.X) + (m_v[1].Y - p.Y) * (m_v[1].Y - p.Y);
			double distC = (m_v[2].X - p.X) * (m_v[2].X - p.X) + (m_v[2].Y - p.Y) * (m_v[2].Y - p.Y);

			if((distA <= VERY_SMALL_DIST) || (distB <= VERY_SMALL_DIST) || (distC <= VERY_SMALL_DIST))
				return true;

			return false;
		}

		bool Overlaps(Extent2D extent)
		{
			// Check simplist case - triangle inside extent.
			if (extent.Contains(m_extent))
				return true;

			// Use right-hand rule for each triangle segment against all points of extent rectangle.
			DelaunayPoint p1 = new DelaunayPoint(extent.MinX, extent.MinY, 0.0f, 0);
			DelaunayPoint p2 = new DelaunayPoint(extent.MaxX, extent.MinY, 0.0f, 0);
			DelaunayPoint p3 = new DelaunayPoint(extent.MaxX, extent.MaxY, 0.0f, 0);
			DelaunayPoint p4 = new DelaunayPoint(extent.MinX, extent.MaxY, 0.0f, 0);

			int ar = 0;
			if (DelaunayGeometry.CrossProduct(m_v[0], p1, m_v[1]) >= 0.0f)
				++ar;
			if (DelaunayGeometry.CrossProduct(m_v[0], p2, m_v[1]) >= 0.0f)
				++ar;
			if (DelaunayGeometry.CrossProduct(m_v[0], p3, m_v[1]) >= 0.0f)
				++ar;
			if (DelaunayGeometry.CrossProduct(m_v[0], p4, m_v[1]) >= 0.0f)
				++ar;

			int br = 0;
			if (DelaunayGeometry.CrossProduct(m_v[1], p1, m_v[2]) >= 0.0f)
				++br;
			if (DelaunayGeometry.CrossProduct(m_v[1], p2, m_v[2]) >= 0.0f)
				++br;
			if (DelaunayGeometry.CrossProduct(m_v[1], p3, m_v[2]) >= 0.0f)
				++br;
			if (DelaunayGeometry.CrossProduct(m_v[1], p4, m_v[2]) >= 0.0f)
				++br;

			int cr = 0;
			if (DelaunayGeometry.CrossProduct(m_v[2], p1, m_v[0]) >= 0.0f)
				++cr;
			if (DelaunayGeometry.CrossProduct(m_v[2], p2, m_v[0]) >= 0.0f)
				++cr;
			if (DelaunayGeometry.CrossProduct(m_v[2], p3, m_v[0]) >= 0.0f)
				++cr;
			if (DelaunayGeometry.CrossProduct(m_v[2], p4, m_v[0]) >= 0.0f)
				++cr;

			// There will be some sort intersection of triangle with rectangle if at least one point of
			// the rectangle is on, or to the right (not zero) of every segment of the triangle.
			if (ar != 0 && br != 0 && cr != 0)
			{
				return true;
			}

			return false;
		}

		bool IsContainedBy(Extent2D extent)
		{
			if (extent.Contains(m_v[0].X, m_v[0].Y) && extent.Contains(m_v[1].X, m_v[1].Y) && extent.Contains(m_v[2].X, m_v[2].Y))
			{
				return true;
			}

			return false;
		}
	}
}
