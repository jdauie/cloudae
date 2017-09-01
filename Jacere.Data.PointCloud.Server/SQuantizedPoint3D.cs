using System;
using System.IO;
using System.Runtime.InteropServices;

namespace Jacere.Data.PointCloud.Server
{
    [StructLayout(LayoutKind.Sequential)]
    public struct SQuantizedPoint3D : IComparable<SQuantizedPoint3D>, IQuantizedPoint3D
    {
        public int X;
        public int Y;
        public int Z;

        public SQuantizedPoint3D(int x, int y, int z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public SQuantizedPoint3D(Point3D point)
        {
            X = (int)point.X;
            Y = (int)point.Y;
            Z = (int)point.Z;
        }

        public SQuantizedPoint3D(BinaryReader reader)
        {
            X = reader.ReadInt32();
            Y = reader.ReadInt32();
            Z = reader.ReadInt32();
        }
        
        public Point3D GetPoint3D()
        {
            return new Point3D(X, Y, Z);
        }

        public int CompareTo(SQuantizedPoint3D other)
        {
            var cmp = X.CompareTo(other.X);
            if (cmp == 0)
            {
                cmp = Y.CompareTo(other.Y);
                if (cmp == 0)
                    cmp = Z.CompareTo(other.Z);
            }
            return cmp;
        }
        
        public override string ToString()
        {
            return string.Format("({0}, {1}, {2})", X, Y, Z);
        }
    }
}
