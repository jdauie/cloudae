using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;

namespace CloudAE.Core.Geometry
{
	/// <summary>
	/// Immutable quantized extent class.
	/// </summary>
	public class UQuantizedExtent3D : UQuantizedExtent2D, IQuantizedExtent3D
	{
		public readonly uint MinZ;

		public readonly uint MaxZ;

		#region Properties

		public uint RangeZ
		{
			get { return MaxZ - MinZ; }
		}

		#endregion

		public UQuantizedExtent3D(uint minX, uint minY, uint minZ, uint maxX, uint maxY, uint maxZ)
			: base(minX, minY, maxX, maxY)
		{
			MinZ = minZ;
			MaxZ = maxZ;
		}

		public UQuantizedExtent3D(BinaryReader reader)
			: base(reader)
		{
			MinZ = reader.ReadUInt32();
			MaxZ = reader.ReadUInt32();
		}

		public override void Serialize(BinaryWriter writer)
		{
			base.Serialize(writer);
			writer.Write(MinZ);
			writer.Write(MaxZ);
		}

		public UQuantizedExtent3D Union(UQuantizedExtent3D extent)
		{
			return new UQuantizedExtent3D(
				Math.Min(MinX, extent.MinX),
				Math.Min(MinY, extent.MinY),
				Math.Min(MinZ, extent.MinZ),
				Math.Max(MaxX, extent.MaxX),
				Math.Max(MaxY, extent.MaxY),
				Math.Max(MaxZ, extent.MaxZ)
			);
		}

		public UQuantizedExtent3D Union2D(UQuantizedExtent3D extent)
		{
			return new UQuantizedExtent3D(
				Math.Min(MinX, extent.MinX),
				Math.Min(MinY, extent.MinY),
				MinZ,
				Math.Max(MaxX, extent.MaxX),
				Math.Max(MaxY, extent.MaxY),
				MaxZ
			);
		}

		/// <summary>
		/// Returns a <see cref="System.String"/> that represents this instance.
		/// </summary>
		/// <returns>
		/// A <see cref="System.String"/> that represents this instance.
		/// </returns>
		public override string ToString()
		{
			return String.Format("({0}, {1}, {2})", RangeX, RangeY, RangeZ);
		}
	}

	public static class UQuantizedExtent3DExtensions
	{
		public static UQuantizedExtent3D Union(this IEnumerable<UQuantizedExtent3D> values)
		{
			UQuantizedExtent3D union = values.First();
			foreach (UQuantizedExtent3D value in values.Skip(1))
				union = union.Union(value);
			return union;
		}

		public static UQuantizedExtent3D Union2D(this IEnumerable<UQuantizedExtent3D> values)
		{
			UQuantizedExtent3D union = values.First();
			foreach (UQuantizedExtent3D value in values.Skip(1))
				union = union.Union2D(value);
			return union;
		}
	}
}
