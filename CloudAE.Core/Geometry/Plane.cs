using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media.Media3D;

namespace CloudAE.Core.Geometry
{
	/// <summary>
	/// Plane defined by Normal and Centroid.
	/// </summary>
	public class Plane
	{
		/// <summary>Centroid.</summary>
		public readonly Point3D Centroid;

		/// <summary>Normal.</summary>
		public readonly Vector3D Normal;

		/// <summary>Unit Normal.</summary>
		public readonly Vector3D UnitNormal;

		/// <summary>
		/// Initializes a new instance of the <see cref="Plane"/> class.
		/// </summary>
		/// <param name="centroid">The centroid.</param>
		/// <param name="normal">The normal.</param>
		public Plane(Point3D centroid, Vector3D normal)
		{
			Centroid = centroid;
			Normal = normal;
			UnitNormal = Normal;
			UnitNormal.Normalize();
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="Plane"/> class.
		/// </summary>
		/// <param name="p1">The p1.</param>
		/// <param name="p2">The p2.</param>
		/// <param name="p3">The p3.</param>
		public Plane(Point3D p1, Point3D p2, Point3D p3)
			: this(p1, p2, p3, false)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="Plane"/> class.
		/// </summary>
		/// <param name="p1">The p1.</param>
		/// <param name="p2">The p2.</param>
		/// <param name="p3">The p3.</param>
		/// <param name="upright">Whether the normal should be oriented up in the Z.</param>
		public Plane(Point3D p1, Point3D p2, Point3D p3, bool upright)
		{
			double x1 = p1.X;
			double x2 = p2.X;
			double x3 = p3.X;
			double y1 = p1.Y;
			double y2 = p2.Y;
			double y3 = p3.Y;
			double z1 = p1.Z;
			double z2 = p2.Z;
			double z3 = p3.Z;

			double cx = (p1.X + p2.X + p3.X) / 3;
			double cy = (p1.Y + p2.Y + p3.Y) / 3;
			double cz = (p1.Z + p2.Z + p3.Z) / 3;

			Centroid = new Point3D(cx, cy, cz);

			Vector3D p12 = new Vector3D(p2.X - p1.X, p2.Y - p1.Y, p2.Z - p1.Z);
			Vector3D p13 = new Vector3D(p3.X - p1.X, p3.Y - p1.Y, p3.Z - p1.Z);
			Normal = Vector3D.CrossProduct(p12, p13);

			if (upright)
			{
				if (Normal.Z < 0)
					Normal = Vector3D.Multiply(Normal, -1);
			}

			UnitNormal = Normal;
			UnitNormal.Normalize();
		}

		/// <summary>
		/// Distance to point.
		/// </summary>
		/// <param name="point">The point.</param>
		/// <returns></returns>
		public double DistanceToPoint(Point3D point)
		{
			double A = UnitNormal.X;
			double B = UnitNormal.Y;
			double C = UnitNormal.Z;
			double D = -1 * (A * Centroid.X + B * Centroid.Y + C * Centroid.Z);
			double signed_distance = A * point.X + B * point.Y + C * point.Z + D;
			return signed_distance;
		}

		/// <summary>
		/// Solves for a point on the plane.
		/// </summary>
		/// <param name="x">The x.</param>
		/// <param name="y">The y.</param>
		/// <returns></returns>
		public double FindPointOnPlane(double x, double y)
		{
			double A = UnitNormal.X;
			double B = UnitNormal.Y;
			double C = UnitNormal.Z;
			double D = -1 * (A * Centroid.X + B * Centroid.Y + C * Centroid.Z);

			double z = -1 * (A * x + B * y + D) / C;
			return z;
		}
	}
}
