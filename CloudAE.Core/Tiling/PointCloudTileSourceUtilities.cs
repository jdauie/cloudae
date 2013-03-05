using System;
using System.Collections.Generic;
using System.Linq;

using Jacere.Core;
using Jacere.Core.Geometry;

namespace CloudAE.Core
{
	public static class PointCloudTileSourceUtilities
	{
		public static System.Windows.Media.Media3D.MeshGeometry3D GenerateMesh(this PointCloudTileSource source, Grid<float> grid, Extent3D distributionExtent)
		{
			return GenerateMesh(source, grid, distributionExtent, false);
		}

		public static System.Windows.Media.Media3D.MeshGeometry3D GenerateMesh(this PointCloudTileSource source, Grid<float> grid, Extent3D distributionExtent, bool showBackFaces)
		{
			// subtract midpoint to center around (0,0,0)
			Extent3D centeringExtent = source.Extent;
			Point3D centerOfMass = source.CenterOfMass;
			double centerOfMassMinusMin = centerOfMass.Z - centeringExtent.MinZ;

			var positions = new System.Windows.Media.Media3D.Point3DCollection(grid.CellCount);
			var indices = new System.Windows.Media.Int32Collection(2 * (grid.SizeX - 1) * (grid.SizeY - 1));

			float fillVal = grid.FillVal;

			for (int x = 0; x < grid.SizeX; x++)
			{
				for (int y = 0; y < grid.SizeY; y++)
				{
					double value = grid.Data[x, y] - centerOfMassMinusMin;

					double xCoord = ((double)x / grid.SizeX) * distributionExtent.RangeX + distributionExtent.MinX - distributionExtent.MidpointX;
					double yCoord = ((double)y / grid.SizeY) * distributionExtent.RangeY + distributionExtent.MinY - distributionExtent.MidpointY;

					xCoord += (distributionExtent.MidpointX - centeringExtent.MidpointX);
					yCoord += (distributionExtent.MidpointY - centeringExtent.MidpointY);

					var point = new System.Windows.Media.Media3D.Point3D(xCoord, yCoord, value);
					positions.Add(point);

					if (x > 0 && y > 0)
					{
						// add two triangles
						int currentPosition = x * grid.SizeY + y;
						int topPosition = currentPosition - 1;
						int leftPosition = currentPosition - grid.SizeY;
						int topleftPosition = leftPosition - 1;

						if (grid.Data[x - 1, y] != fillVal && grid.Data[x, y - 1] != fillVal)
						{
							if (grid.Data[x, y] != fillVal)
							{
								indices.Add(leftPosition);
								indices.Add(topPosition);
								indices.Add(currentPosition);

								if (showBackFaces)
								{
									indices.Add(leftPosition);
									indices.Add(currentPosition);
									indices.Add(topPosition);
								}
							}

							if (grid.Data[x - 1, y - 1] != fillVal)
							{
								indices.Add(topleftPosition);
								indices.Add(topPosition);
								indices.Add(leftPosition);

								if (showBackFaces)
								{
									indices.Add(topleftPosition);
									indices.Add(leftPosition);
									indices.Add(topPosition);
								}
							}
						}
					}
				}
			}

			var normals = new System.Windows.Media.Media3D.Vector3DCollection(positions.Count);

			for (int i = 0; i < positions.Count; i++)
				normals.Add(new System.Windows.Media.Media3D.Vector3D(0, 0, 0));

			for (int i = 0; i < indices.Count; i += 3)
			{
				int index1 = indices[i];
				int index2 = indices[i + 1];
				int index3 = indices[i + 2];

				System.Windows.Media.Media3D.Vector3D side1 = positions[index1] - positions[index3];
				System.Windows.Media.Media3D.Vector3D side2 = positions[index1] - positions[index2];
				System.Windows.Media.Media3D.Vector3D normal = System.Windows.Media.Media3D.Vector3D.CrossProduct(side1, side2);

				normals[index1] += normal;
				normals[index2] += normal;
				normals[index3] += normal;
			}

			for (int i = 0; i < normals.Count; i++)
			{
				if (normals[i].Length > 0)
				{
					var normal = normals[i];
					normal.Normalize();

					// the fact that this is necessary means I am doing something wrong
					if (normal.Z < 0)
						normal.Negate();

					normals[i] = normal;
				}
			}

			var geometry = new System.Windows.Media.Media3D.MeshGeometry3D
			{
				Positions = positions,
				TriangleIndices = indices,
				Normals = normals
			};

			return geometry;
		}

		public static unsafe System.Windows.Media.Media3D.MeshGeometry3D LoadTilePointMesh(PointCloudTile tile, byte[] inputBuffer, double pointSize, int thinByFactor)
		{
			/*LoadTile(tile, inputBuffer);

			Extent3D distributionExtent = tile.Extent;
			Extent3D centeringExtent = Extent;
			Point3D centerOfMass = CenterOfMass;

			double xShift = -centeringExtent.MidpointX;
			double yShift = -centeringExtent.MidpointY;
			double zShift = -centerOfMass.Z;

			double halfPointSize = pointSize / 2;

			int thinnedTilePointCount = tile.PointCount / thinByFactor;

			// these values need to be changed from 4,6 to 8,36 if I test it out
			System.Windows.Media.Media3D.Point3DCollection positions = new System.Windows.Media.Media3D.Point3DCollection(thinnedTilePointCount * 4);
			System.Windows.Media.Media3D.Vector3DCollection normals = new System.Windows.Media.Media3D.Vector3DCollection(positions.Count);
			System.Windows.Media.Int32Collection indices = new System.Windows.Media.Int32Collection(tile.PointCount * 6);

			fixed (byte* inputBufferPtr = inputBuffer)
			{
				UQuantizedPoint3D* p = (UQuantizedPoint3D*)inputBufferPtr;

				for (int i = 0; i < tile.PointCount; i++)
				{
					if (!(i % thinByFactor == 0))
						continue;

					// slow!
					Point3D point = Quantization.Convert(p[i]);
					double xC = point.X + xShift;
					double yC = point.Y + yShift;
					double zC = point.Z + zShift;

					int currentStartIndex = positions.Count;

					foreach (double y in new double[] { yC - halfPointSize, yC + halfPointSize })
					{
						foreach (double x in new double[] { xC - halfPointSize, xC + halfPointSize })
						{
							positions.Add(new System.Windows.Media.Media3D.Point3D(x, y, zC));
							normals.Add(new System.Windows.Media.Media3D.Vector3D(0, 0, 1));
						}
					}

					indices.Add(currentStartIndex + 0);
					indices.Add(currentStartIndex + 1);
					indices.Add(currentStartIndex + 3);

					indices.Add(currentStartIndex + 0);
					indices.Add(currentStartIndex + 3);
					indices.Add(currentStartIndex + 2);
				}
			}

			System.Windows.Media.Media3D.MeshGeometry3D geometry = new System.Windows.Media.Media3D.MeshGeometry3D();
			geometry.Positions = positions;
			geometry.TriangleIndices = indices;
			geometry.Normals = normals;

			return geometry;*/

			return null;
		}

		public static unsafe System.Windows.Media.Media3D.MeshGeometry3D LoadTileMeshDelaunayIncremental(PointCloudTile tile, byte[] inputBuffer)
		{
			//Open();

			//Extent3D extent = tile.Extent;

			//DelaunayPoint[] pointsToTriangulate = new DelaunayPoint[tile.PointCount];

			//fixed (byte* inputBufferPtr = inputBuffer)
			//{
			//    UQuantizedPoint3D* p = (UQuantizedPoint3D*)inputBufferPtr;

			//    int bytesRead = tile.ReadTile(m_inputStream, inputBuffer);

			//    for (int i = 0; i < tile.PointCount; i++)
			//    {
			//        Point3D point = Quantization.Convert(p[i]);

			//        pointsToTriangulate[i] = new DelaunayPoint(
			//            (float)point.X,
			//            (float)point.Y,
			//            (float)point.Z,
			//            i);
			//    }
			//}

			//Delaunay2DIncremental triangulator = new Delaunay2DIncremental();
			//triangulator.Initialize(extent, pointsToTriangulate.Length);

			//foreach (DelaunayPoint point in pointsToTriangulate)
			//{
			//    if (triangulator.Locate(point))
			//        triangulator.UpdateTriangle(point);
			//}

			//List<int> mesh = triangulator.FlushTriangles();

			//System.Windows.Media.Media3D.Point3DCollection points = new System.Windows.Media.Media3D.Point3DCollection(pointsToTriangulate.Length);
			//for (int i = 0; i < pointsToTriangulate.Length; i++)
			//    points.Add(new System.Windows.Media.Media3D.Point3D(
			//        pointsToTriangulate[i].X - Extent.MidpointX,
			//        pointsToTriangulate[i].Y - Extent.MidpointY,
			//        pointsToTriangulate[i].Z - Extent.MidpointZ));

			//System.Windows.Media.Int32Collection triangles = new System.Windows.Media.Int32Collection(mesh.Reverse<int>());

			//System.Windows.Media.Media3D.MeshGeometry3D meshGeometry = new System.Windows.Media.Media3D.MeshGeometry3D();
			//meshGeometry.Positions = points;
			//meshGeometry.TriangleIndices = triangles;

			//return meshGeometry;

			return null;
		}
	}
}
