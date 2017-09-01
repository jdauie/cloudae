using System.Linq;
using System.IO;
using System.Collections.Generic;

namespace Jacere.Data.PointCloud.Server
{
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
        
        public Extent3D(Point3D min, Point3D max)
            : base(min.X, min.Y, max.X, max.Y)
        {
            MinZ = min.Z;
            MaxZ = max.Z;
        }

        public double RangeZ => MaxZ - MinZ;

        public double MidpointZ => (MaxZ + MinZ) / 2;

        public Point3D GetMinPoint3D()
        {
            return new Point3D(MinX, MinY, MinZ);
        }

        public Point3D GetMaxPoint3D()
        {
            return new Point3D(MaxX, MaxY, MaxZ);
        }

        public override string ToString()
        {
            return string.Format("({0:f}, {1:f}, {2:f})", RangeX, RangeY, RangeZ);
        }
    }

    public static class Extent3DExtensions
    {
        public static Extent3D Union3D(this IEnumerable<Extent3D> source)
        {
            var extents = source.ToList();

            return new Extent3D(
                extents.Min(e => e.MinX),
                extents.Min(e => e.MinY),
                extents.Min(e => e.MinZ),
                extents.Max(e => e.MaxX),
                extents.Max(e => e.MaxY),
                extents.Max(e => e.MaxZ)
            );
        }
    }
}
