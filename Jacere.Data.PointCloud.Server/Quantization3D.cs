using System;
using System.IO;

namespace Jacere.Data.PointCloud.Server
{
    public abstract class Quantization3D<TPoint, TExtent> 
        : IEquatable<Quantization3D<TPoint, TExtent>>
        where TPoint : IQuantizedPoint3D
        where TExtent : IQuantizedExtent3D
    {
        protected readonly Point3D Offset;
        protected readonly Point3D ScaleFactor;
        protected readonly Point3D ScaleFactorInverse;

        public double OffsetX => Offset.X;
        public double OffsetY => Offset.Y;
        public double OffsetZ => Offset.Z;

        public double ScaleFactorX => ScaleFactor.X;
        public double ScaleFactorY => ScaleFactor.Y;
        public double ScaleFactorZ => ScaleFactor.Z;

        public double ScaleFactorInverseX => ScaleFactorInverse.X;
        public double ScaleFactorInverseY => ScaleFactorInverse.Y;
        public double ScaleFactorInverseZ => ScaleFactorInverse.Z;
        
        protected Quantization3D(double sfX, double sfY, double sfZ, double oX, double oY, double oZ)
        {
            ScaleFactor = new Point3D(sfX, sfY, sfZ);
            Offset = new Point3D(oX, oY, oZ);

            ScaleFactorInverse = 1.0 / ScaleFactor;
        }

        protected Quantization3D(BinaryReader reader)
        {
            ScaleFactor = reader.ReadPoint3D();
            Offset = reader.ReadPoint3D();

            ScaleFactorInverse = 1.0 / ScaleFactor;
        }
        
        public bool Equals(Quantization3D<TPoint, TExtent> other)
        {
            return Offset == other.Offset && ScaleFactor == other.ScaleFactor;
        }
        
        public Point3D Convert(TPoint point)
        {
            return point.GetPoint3D() * ScaleFactor + Offset;
        }

        public Extent3D Convert(TExtent extent)
        {
            var e = extent.GetExtent3D();
            return new Extent3D(
                e.GetMinPoint3D() * ScaleFactor + Offset,
                e.GetMaxPoint3D() * ScaleFactor + Offset
            );
        }
    }
}
