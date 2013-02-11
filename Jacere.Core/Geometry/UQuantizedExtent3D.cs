using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;

namespace Jacere.Core.Geometry
{
	/// <summary>
	/// Immutable quantized extent class.
	/// </summary>
    [Obsolete("Moving back to LAS compatibility", true)]
	public class UQuantizedExtent3D : IQuantizedExtent3D
	{
		private readonly UQuantizedPoint3D m_min;
		private readonly UQuantizedPoint3D m_max;

		#region Properties

		public IQuantizedPoint3D Min
		{
			get { return m_min; }
		}

		public IQuantizedPoint3D Max
		{
			get { return m_max; }
		}

		public uint MinX
		{
			get { return m_min.X; }
		}

		public uint MinY
		{
			get { return m_min.Y; }
		}

		public uint MinZ
		{
			get { return m_min.Z; }
		}

		public uint MaxX
		{
			get { return m_max.X; }
		}

		public uint MaxY
		{
			get { return m_max.Y; }
		}

		public uint MaxZ
		{
			get { return m_max.Z; }
		}

		public uint RangeX
		{
			get { return m_max.X - m_min.X; }
		}

		public uint RangeY
		{
			get { return m_max.Y - m_min.Y; }
		}

		public uint RangeZ
		{
			get { return m_max.Z - m_min.Z; }
		}

		#endregion

		public UQuantizedExtent3D(uint minX, uint minY, uint minZ, uint maxX, uint maxY, uint maxZ)
		{
			m_min = new UQuantizedPoint3D(minX, minY, minZ);
			m_max = new UQuantizedPoint3D(maxX, maxY, maxZ);
		}

		public UQuantizedExtent3D(UQuantizedPoint3D min, UQuantizedPoint3D max)
		{
			m_min = min;
			m_max = max;
		}

		public UQuantizedExtent3D(Extent3D extent)
		{
			m_min = new UQuantizedPoint3D((uint)extent.MinX, (uint)extent.MinY, (uint)extent.MinZ);
			m_max = new UQuantizedPoint3D((uint)extent.MaxX, (uint)extent.MaxY, (uint)extent.MaxZ);
		}

		public UQuantizedExtent3D(BinaryReader reader)
		{
			m_min = reader.ReadUQuantizedPoint3D();
			m_max = reader.ReadUQuantizedPoint3D();
		}

		public void Serialize(BinaryWriter writer)
		{
			writer.Write(m_min);
			writer.Write(m_max);
		}

		public Extent3D GetExtent3D()
		{
			return new Extent3D(Min.GetPoint3D(), Max.GetPoint3D());
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
        [Obsolete("Moving back to LAS compatibility", true)]
		public static UQuantizedExtent3D Union(this IEnumerable<UQuantizedExtent3D> values)
		{
			UQuantizedExtent3D union = values.First();
			foreach (UQuantizedExtent3D value in values.Skip(1))
				union = union.Union(value);
			return union;
		}
	}
}
