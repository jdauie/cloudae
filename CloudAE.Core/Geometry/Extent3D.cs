using System;
using System.Linq;
using System.IO;
using System.Collections.Generic;

namespace CloudAE.Core.Geometry
{
	/// <summary>
	/// Immutable extent class.
	/// </summary>
	public class Extent3D : Extent2D
	{
		public readonly double MinZ;
		public readonly double MaxZ;

		public Extent3D(double minX, double minY, double minZ, double maxX, double maxY, double maxZ)
			: base(minX, minY, maxX, maxY)
		{
			MinZ = minZ;
			MaxZ = maxZ;
		}

		public Extent3D(Point3D min, Point3D max)
			: base(min.X, min.Y, max.X, max.Y)
		{
			MinZ = min.Z;
			MaxZ = max.Z;
		}

		public Extent3D(BinaryReader reader)
			: base(reader)
		{
			MaxZ = reader.ReadDouble();
			MinZ = reader.ReadDouble();
		}

		public override void Serialize(BinaryWriter writer)
		{
			base.Serialize(writer);
			writer.Write(MaxZ);
			writer.Write(MinZ);
		}

		public double RangeZ
		{
			get { return MaxZ - MinZ; }
		}

		public double MidpointZ
		{
			get { return (MaxZ + MinZ) / 2; }
		}

		public Point3D GetMinPoint3D()
		{
			return new Point3D(MinX, MinY, MinZ);
		}

		public Point3D GetMaxPoint3D()
		{
			return new Point3D(MaxX, MaxY, MaxZ);
		}

		/// <summary>
		/// Returns a <see cref="System.String"/> that represents this instance.
		/// </summary>
		/// <returns>
		/// A <see cref="System.String"/> that represents this instance.
		/// </returns>
		public override string ToString()
		{
			return String.Format("({0:f}, {1:f}, {2:f})", RangeX, RangeY, RangeZ);
		}

		public override bool Equals(object obj)
		{
			var extent = obj as Extent3D;
			return (extent != null &&
				extent.MinX == MinX &&
				extent.MinY == MinY &&
				extent.MinZ == MinZ &&
				extent.MaxX == MaxX &&
				extent.MaxY == MaxY &&
				extent.MaxZ == MaxZ
			);
		}
	}

	public static class Extent3DExtensions
	{
		public static Extent3D Union3D(this IEnumerable<Extent3D> source)
		{
			Extent3D[] extents = source.ToArray();

			return new Extent3D(
				extents.Min(e => e.MinX),
				extents.Min(e => e.MinY),
				extents.Min(e => e.MinZ),
				extents.Max(e => e.MaxX),
				extents.Max(e => e.MaxY),
				extents.Max(e => e.MaxZ)
			);
		}
	}
}
