using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CloudAE.Core.Geometry
{
	public class SQuantization3D : Quantization3D
	{
		public SQuantization3D(double sfX, double sfY, double sfZ, double oX, double oY, double oZ)
			: base(sfX, sfY, sfZ, oX, oY, oZ)
		{
		}

		public override IQuantizedPoint3D Convert(Point3D point)
		{
			return new SQuantizedPoint3D(
				(int)((point.X - OffsetX) / ScaleFactorX),
				(int)((point.Y - OffsetY) / ScaleFactorY),
				(int)((point.Z - OffsetZ) / ScaleFactorZ)
			);
		}

		public override Point3D Convert(IQuantizedPoint3D point)
		{
			if (!(point is SQuantizedPoint3D))
				throw new ArgumentException("Signed quantization requires signed points", "point");

			SQuantizedPoint3D qPoint = (SQuantizedPoint3D)point;
			return new Point3D(
				qPoint.X * ScaleFactorX + OffsetX,
				qPoint.Y * ScaleFactorY + OffsetY,
				qPoint.Z * ScaleFactorZ + OffsetZ
			);
		}

		//public unsafe void Convert(byte* inputBufferPtr, int pointCount, int pointSize, Point3D* pointBufferPtr)
		//{
		//    for (int i = 0; i < pointCount; ++i)
		//    {
		//        SQuantizedPoint3D point = *(SQuantizedPoint3D*)(inputBufferPtr + i * pointSize);

		//        pointBufferPtr[i] = new Point3D(
		//            point.X * ScaleFactorX + OffsetX,
		//            point.Y * ScaleFactorY + OffsetY,
		//            point.Z * ScaleFactorZ + OffsetZ
		//        );
		//    }
		//}

		public override IQuantizedExtent3D Convert(Extent3D extent)
		{
			int minX = ConvertXValToQuantized(extent.MinX);
			int minY = ConvertYValToQuantized(extent.MinY);
			int minZ = ConvertZValToQuantized(extent.MinZ);
			int maxX = ConvertXValToQuantized(extent.MaxX);
			int maxY = ConvertYValToQuantized(extent.MaxY);
			int maxZ = ConvertZValToQuantized(extent.MaxZ);

			return new SQuantizedExtent3D(minX, minY, minZ, maxX, maxY, maxZ);
		}

		public override Extent3D Convert(IQuantizedExtent3D extent)
		{
			if (!(extent is SQuantizedExtent3D))
				throw new ArgumentException("Signed quantization requires signed extent", "extent");

			SQuantizedExtent3D sExtent = (SQuantizedExtent3D)extent;

			double minX = ConvertXValFromQuantized(sExtent.MinX);
			double minY = ConvertYValFromQuantized(sExtent.MinY);
			double minZ = ConvertZValFromQuantized(sExtent.MinZ);
			double maxX = ConvertXValFromQuantized(sExtent.MaxX);
			double maxY = ConvertYValFromQuantized(sExtent.MaxY);
			double maxZ = ConvertZValFromQuantized(sExtent.MaxZ);

			return new Extent3D(minX, minY, minZ, maxX, maxY, maxZ);
		}

		public int ConvertXValToQuantized(double x)
		{
			return (int)((x - OffsetX) / ScaleFactorX);
		}

		public int ConvertYValToQuantized(double y)
		{
			return (int)((y - OffsetY) / ScaleFactorY);
		}

		public int ConvertZValToQuantized(double z)
		{
			return (int)((z - OffsetZ) / ScaleFactorZ);
		}

		public double ConvertXValFromQuantized(int x)
		{
			return x * ScaleFactorX + OffsetX;
		}

		public double ConvertYValFromQuantized(int y)
		{
			return y * ScaleFactorY + OffsetY;
		}

		public double ConvertZValFromQuantized(int z)
		{
			return z * ScaleFactorZ + OffsetZ;
		}
	}
}
