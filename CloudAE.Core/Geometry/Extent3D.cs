using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

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

		public Extent3D(BinaryReader reader)
			: base(reader)
		{
			MinZ = reader.ReadDouble();
			MaxZ = reader.ReadDouble();
		}

		public override void Serialize(BinaryWriter writer)
		{
			base.Serialize(writer);
			writer.Write(MinZ);
			writer.Write(MaxZ);
		}

		public double RangeZ
		{
			get { return MaxZ - MinZ; }
		}

		public double MidpointZ
		{
			get { return (MaxZ + MinZ) / 2; }
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
			Extent3D extent = obj as Extent3D;
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
}
