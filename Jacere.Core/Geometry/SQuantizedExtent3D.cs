using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Jacere.Core.Geometry
{
	/// <summary>
	/// Immutable quantized extent class.
	/// </summary>
	public class SQuantizedExtent3D : IQuantizedExtent3D
	{
		private readonly SQuantizedPoint3D m_min;
		private readonly SQuantizedPoint3D m_max;

		#region Properties

		public IQuantizedPoint3D Min
		{
			get { return m_min; }
		}

		public IQuantizedPoint3D Max
		{
			get { return m_max; }
		}

		public int MinX
		{
			get { return m_min.X; }
		}

		public int MinY
		{
			get { return m_min.Y; }
		}

		public int MinZ
		{
			get { return m_min.Z; }
		}

		public int MaxX
		{
			get { return m_max.X; }
		}

		public int MaxY
		{
			get { return m_max.Y; }
		}

		public int MaxZ
		{
			get { return m_max.Z; }
		}

		public uint RangeX
		{
			get { return (uint)(m_max.X - m_min.X); }
		}

		public uint RangeY
		{
			get { return (uint)(m_max.Y - m_min.Y); }
		}

		public uint RangeZ
		{
			get { return (uint)(m_max.Z - m_min.Z); }
		}

		#endregion

		public SQuantizedExtent3D(int minX, int minY, int minZ, int maxX, int maxY, int maxZ)
		{
			m_min = new SQuantizedPoint3D(minX, minY, minZ);
			m_max = new SQuantizedPoint3D(maxX, maxY, maxZ);
		}

		public SQuantizedExtent3D(SQuantizedPoint3D min, SQuantizedPoint3D max)
		{
			m_min = min;
			m_max = max;
		}

		public SQuantizedExtent3D(Extent3D extent)
		{
			m_min = new SQuantizedPoint3D((int)extent.MinX, (int)extent.MinY, (int)extent.MinZ);
			m_max = new SQuantizedPoint3D((int)extent.MaxX, (int)extent.MaxY, (int)extent.MaxZ);
		}

		public SQuantizedExtent3D(BinaryReader reader)
		{
			m_min = reader.ReadSQuantizedPoint3D();
			m_max = reader.ReadSQuantizedPoint3D();
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
}
