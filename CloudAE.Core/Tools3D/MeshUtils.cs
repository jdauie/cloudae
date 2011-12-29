using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using System.Diagnostics;
using System.Windows;

namespace CloudAE.Core.Tools3D
{
	public class MeshUtils
	{
		public static PointCollection GeneratePlanarTextureCoordinates(MeshGeometry3D mesh, Vector3D dir)
		{
			if (mesh == null)
				return null;

			return GeneratePlanarTextureCoordinates(mesh, mesh.Bounds, dir);
		}

		public static PointCollection GeneratePlanarTextureCoordinates(MeshGeometry3D mesh, Rect3D bounds, Vector3D dir)
		{
			if (mesh == null)
				return null;

			//if (!bounds.Contains(mesh.Bounds))
			//    throw new ArgumentException("bounds must fully contain mesh.Bounds", "bounds");

			int count = mesh.Positions.Count;
			PointCollection texcoords = new PointCollection(count);
			IEnumerable<Point3D> positions = TransformPoints(ref bounds, mesh.Positions, ref dir);

			foreach (Point3D vertex in positions)
			{
				// The plane is looking along positive Y, so Z is really Y

				texcoords.Add(new Point(
					GetPlanarCoordinate(vertex.X, bounds.X, bounds.SizeX),
					GetPlanarCoordinate(vertex.Z, bounds.Z, bounds.SizeZ)
					));
			}

			return texcoords;
		}

		internal static double GetPlanarCoordinate(double end, double start, double width)
		{
			return (end - start) / width;
		}

		internal static IEnumerable<Point3D> TransformPoints(ref Rect3D bounds, Point3DCollection points, ref Vector3D dir)
		{
			if (dir == MathUtils.YAxis)
			{
				return points;
			}

			Vector3D rotAxis = Vector3D.CrossProduct(dir, MathUtils.YAxis);
			double rotAngle = Vector3D.AngleBetween(dir, MathUtils.YAxis);
			Quaternion q;

			if (rotAxis.X != 0 || rotAxis.Y != 0 || rotAxis.Z != 0)
			{
				Debug.Assert(rotAngle != 0);

				q = new Quaternion(rotAxis, rotAngle);
			}
			else
			{
				Debug.Assert(dir == -MathUtils.YAxis);

				q = new Quaternion(MathUtils.XAxis, rotAngle);
			}

			Vector3D center = new Vector3D(
				bounds.X + bounds.SizeX / 2,
				bounds.Y + bounds.SizeY / 2,
				bounds.Z + bounds.SizeZ / 2
				);

			Matrix3D t = Matrix3D.Identity;
			t.Translate(-center);
			t.Rotate(q);

			int count = points.Count;
			Point3D[] transformedPoints = new Point3D[count];

			for (int i = 0; i < count; i++)
			{
				transformedPoints[i] = t.Transform(points[i]);
			}

			// Finally, transform the bounds too
			bounds = MathUtils.TransformBounds(bounds, t);

			return transformedPoints;
		}
	}
}
