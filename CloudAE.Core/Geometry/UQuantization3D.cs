using System;
using System.Linq;
using System.IO;

namespace CloudAE.Core.Geometry
{
	public class UQuantization3D : Quantization3D
	{
		public UQuantization3D(double sfX, double sfY, double sfZ, double oX, double oY, double oZ)
			: base(sfX, sfY, sfZ, oX, oY, oZ)
		{
		}

		public UQuantization3D(BinaryReader reader)
			: base(reader)
		{
		}

		public override IQuantizedPoint3D Convert(Point3D point)
		{
			return new UQuantizedPoint3D(
				(uint)((point.X - OffsetX) / ScaleFactorX),
				(uint)((point.Y - OffsetY) / ScaleFactorY),
				(uint)((point.Z - OffsetZ) / ScaleFactorZ)
			);
		}

		public override Point3D Convert(IQuantizedPoint3D point)
		{
			if (!(point is UQuantizedPoint3D))
				throw new ArgumentException("Unsigned quantization requires unsigned points", "point");

			UQuantizedPoint3D qPoint = (UQuantizedPoint3D)point;
			return new Point3D(
				qPoint.X * ScaleFactorX + OffsetX,
				qPoint.Y * ScaleFactorY + OffsetY,
				qPoint.Z * ScaleFactorZ + OffsetZ
			);
		}

		public override IQuantizedExtent3D Convert(Extent3D extent)
		{
			uint minX = ConvertXValToQuantized(extent.MinX);
			uint minY = ConvertYValToQuantized(extent.MinY);
			uint minZ = ConvertZValToQuantized(extent.MinZ);
			uint maxX = ConvertXValToQuantized(extent.MaxX);
			uint maxY = ConvertYValToQuantized(extent.MaxY);
			uint maxZ = ConvertZValToQuantized(extent.MaxZ);

			return new UQuantizedExtent3D(minX, minY, minZ, maxX, maxY, maxZ);
		}

		public override Extent3D Convert(IQuantizedExtent3D extent)
		{
			if (!(extent is UQuantizedExtent3D))
				throw new ArgumentException("Unsigned quantization requires unsigned extent", "extent");

			UQuantizedExtent3D sExtent = (UQuantizedExtent3D)extent;

			double minX = ConvertXValFromQuantized(sExtent.MinX);
			double minY = ConvertYValFromQuantized(sExtent.MinY);
			double minZ = ConvertZValFromQuantized(sExtent.MinZ);
			double maxX = ConvertXValFromQuantized(sExtent.MaxX);
			double maxY = ConvertYValFromQuantized(sExtent.MaxY);
			double maxZ = ConvertZValFromQuantized(sExtent.MaxZ);

			return new Extent3D(minX, minY, minZ, maxX, maxY, maxZ);
		}

		public uint ConvertXValToQuantized(double x)
		{
			return checked((uint)((x - OffsetX) / ScaleFactorX));
		}

		public uint ConvertYValToQuantized(double y)
		{
			return checked((uint)((y - OffsetY) / ScaleFactorY));
		}

		public uint ConvertZValToQuantized(double z)
		{
			return checked((uint)((z - OffsetZ) / ScaleFactorZ));
		}

		public double ConvertXValFromQuantized(uint x)
		{
			return x * ScaleFactorX + OffsetX;
		}

		public double ConvertYValFromQuantized(uint y)
		{
			return y * ScaleFactorY + OffsetY;
		}

		public double ConvertZValFromQuantized(uint z)
		{
			return z * ScaleFactorZ + OffsetZ;
		}
	}
}
