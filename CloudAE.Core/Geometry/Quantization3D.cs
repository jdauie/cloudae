using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace CloudAE.Core.Geometry
{
	public abstract class Quantization3D : IQuantization3D, ISerializeBinary
	{
		public readonly double ScaleFactorX;
		public readonly double ScaleFactorY;
		public readonly double ScaleFactorZ;
		public readonly double OffsetX;
		public readonly double OffsetY;
		public readonly double OffsetZ;

		public Quantization3D(double sfX, double sfY, double sfZ, double oX, double oY, double oZ)
		{
			ScaleFactorX = sfX;
			ScaleFactorY = sfY;
			ScaleFactorZ = sfZ;
			OffsetX = oX;
			OffsetY = oY;
			OffsetZ = oZ;
		}

		public Quantization3D(BinaryReader reader)
		{
			ScaleFactorX = reader.ReadDouble();
			ScaleFactorY = reader.ReadDouble();
			ScaleFactorZ = reader.ReadDouble();
			OffsetX = reader.ReadDouble();
			OffsetY = reader.ReadDouble();
			OffsetZ = reader.ReadDouble();
		}

		public void Serialize(BinaryWriter writer)
		{
			writer.Write(ScaleFactorX);
			writer.Write(ScaleFactorY);
			writer.Write(ScaleFactorZ);
			writer.Write(OffsetX);
			writer.Write(OffsetY);
			writer.Write(OffsetZ);
		}

		public static Quantization3D Create(Extent3D extent, bool unsigned)
		{
			double qOffsetX = extent.MidpointX;
			double qOffsetY = extent.MidpointY;
			double qOffsetZ = extent.MidpointZ;

			if (unsigned)
			{
				qOffsetX = extent.MinX;
				qOffsetY = extent.MinY;
				qOffsetZ = extent.MinZ;
			}

			// this is a stupid way to do this
			// I need to get the precision evaluation working

			double pow2to32 = Math.Pow(2, 32);
			double logBase = 10; // this value effects debugging and compressibility

			int precisionMaxX = (int)Math.Floor(Math.Log(pow2to32 / (extent.RangeX), logBase));
			int precisionMaxY = (int)Math.Floor(Math.Log(pow2to32 / (extent.RangeY), logBase));
			int precisionMaxZ = (int)Math.Floor(Math.Log(pow2to32 / (extent.RangeZ), logBase));

			double qScaleFactorX = Math.Pow(logBase, -precisionMaxX);
			double qScaleFactorY = Math.Pow(logBase, -precisionMaxY);
			double qScaleFactorZ = Math.Pow(logBase, -precisionMaxZ);

			if(unsigned)
				return new UQuantization3D(qScaleFactorX, qScaleFactorY, qScaleFactorZ, qOffsetX, qOffsetY, qOffsetZ);
			else
				return new SQuantization3D(qScaleFactorX, qScaleFactorY, qScaleFactorZ, qOffsetX, qOffsetY, qOffsetZ);
		}

		public abstract IQuantizedPoint3D Convert(Point3D point);
		public abstract Point3D Convert(IQuantizedPoint3D point);
		public abstract IQuantizedExtent3D Convert(Extent3D extent);
		public abstract Extent3D Convert(IQuantizedExtent3D extent);
	}
}
