using System;
using System.Linq;
using System.IO;

namespace CloudAE.Core.Geometry
{
	/// <summary>
	/// Immutable quantized extent class.
	/// </summary>
	public class UQuantizedExtent2D : IQuantizedExtent2D, ISerializeBinary
	{
		public readonly uint MinX;
		public readonly uint MinY;

		public readonly uint MaxX;
		public readonly uint MaxY;

		#region Properties

		public uint RangeX
		{
			get { return MaxX - MinX; }
		}

		public uint RangeY
		{
			get { return MaxY - MinY; }
		}

		#endregion

		public UQuantizedExtent2D(uint minX, uint minY, uint maxX, uint maxY)
		{
			MinX = minX;
			MinY = minY;
			MaxX = maxX;
			MaxY = maxY;
		}

		public UQuantizedExtent2D(BinaryReader reader)
		{
			MinX = reader.ReadUInt32();
			MaxX = reader.ReadUInt32();
			MinY = reader.ReadUInt32();
			MaxY = reader.ReadUInt32();
		}

		public virtual void Serialize(BinaryWriter writer)
		{
			writer.Write(MinX);
			writer.Write(MaxX);
			writer.Write(MinY);
			writer.Write(MaxY);
		}

		public bool Contains(uint x, uint y)
		{
			return (x >= MinX && x <= MaxX && y >= MinY && y <= MaxY);
		}

		//public unsafe UQuantizedExtent2D(UQuantizedPoint3D* p, int count)
		//{
		//    MinX = uint.MaxValue;
		//    MinY = uint.MaxValue;
		//    MaxX = uint.MinValue;
		//    MaxY = uint.MinValue;

		//    for (int i = 0; i < count; i++)
		//    {
		//        UQuantizedPoint3D qPoint = p[i];

		//        MinX = Math.Min(MinX, qPoint.X);
		//        MinY = Math.Min(MinY, qPoint.Y);
		//        MaxX = Math.Max(MinX, qPoint.X);
		//        MaxY = Math.Max(MinY, qPoint.Y);
		//    }
		//}

		public UQuantizedExtent2D Union(UQuantizedExtent2D extent)
		{
			return new UQuantizedExtent2D(
				Math.Min(MinX, extent.MinX),
				Math.Min(MinY, extent.MinY),
				Math.Max(MaxX, extent.MaxX),
				Math.Max(MaxY, extent.MaxY)
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
			return String.Format("({0}, {1})", RangeX, RangeY);
		}
	}
}
