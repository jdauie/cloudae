using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CloudAE.Core.Geometry
{
	/// <summary>
	/// Immutable quantized extent class.
	/// </summary>
	public class SQuantizedExtent3D : IQuantizedExtent3D
	{
		public readonly int MinX;
		public readonly int MinY;
		public readonly int MinZ;

		public readonly int MaxX;
		public readonly int MaxY;
		public readonly int MaxZ;

		#region Properties

		public uint RangeX
		{
			get { return (uint)((long)MaxX - MinX); }
		}

		public uint RangeY
		{
			get { return (uint)((long)MaxY - MinY); }
		}

		public uint RangeZ
		{
			get { return (uint)((long)MaxZ - MinZ); }
		}

		#endregion

		public SQuantizedExtent3D(int minX, int minY, int minZ, int maxX, int maxY, int maxZ)
		{
			MinX = minX;
			MinY = minY;
			MinZ = minZ;
			MaxX = maxX;
			MaxY = maxY;
			MaxZ = maxZ;
		}

		public unsafe SQuantizedExtent3D(SQuantizedPoint3D* p, int count)
		{
			MinX = int.MaxValue;
			MinY = int.MaxValue;
			MinZ = int.MaxValue;
			MaxX = int.MinValue;
			MaxY = int.MinValue;
			MaxZ = int.MaxValue;

			for (int i = 0; i < count; i++)
			{
				SQuantizedPoint3D qPoint = p[i];

				MinX = Math.Min(MinX, qPoint.X);
				MinY = Math.Min(MinY, qPoint.Y);
				MinZ = Math.Min(MinZ, qPoint.Z);
				MaxX = Math.Max(MinX, qPoint.X);
				MaxY = Math.Max(MinY, qPoint.Y);
				MaxZ = Math.Max(MinZ, qPoint.Z);
			}
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
