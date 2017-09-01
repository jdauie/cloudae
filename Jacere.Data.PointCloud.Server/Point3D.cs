using System;
using System.IO;

namespace Jacere.Data.PointCloud.Server
{
    public class IndexedPoint3D : Point3D
    {
        public readonly long SourceOffset;
        public readonly int SourceLength;

        public IndexedPoint3D(double x, double y, double z, long offset, int length)
            : base(x, y, z)
        {
            SourceOffset = offset;
            SourceLength = length;
        }
    }

    public class Point3D : IEquatable<Point3D>
    {
        public static bool operator ==(Point3D p1, Point3D p2)
        {
            if (ReferenceEquals(p1, p2))
                return true;
            return !ReferenceEquals(p1, null) && p1.Equals(p2);
        }

        public static bool operator !=(Point3D p1, Point3D p2)
        {
            return !(p1 == p2);
        }

        public static Point3D operator +(Point3D p1, Point3D p2)
        {
            return new Point3D(p1.X + p2.X, p1.Y + p2.Y, p1.Z + p2.Z);
        }

        public static Point3D operator -(Point3D p1, Point3D p2)
        {
            return new Point3D(p1.X - p2.X, p1.Y - p2.Y, p1.Z - p2.Z);
        }

        public static Point3D operator *(Point3D p, double m)
        {
            return new Point3D(p.X * m, p.Y * m, p.Z * m);
        }

        public static Point3D operator *(double m, Point3D p)
        {
            return new Point3D(p.X * m, p.Y * m, p.Z * m);
        }

        public static Point3D operator *(Point3D p1, Point3D p2)
        {
            return new Point3D(p1.X * p2.X, p1.Y * p2.Y, p1.Z * p2.Z);
        }

        public static Point3D operator /(Point3D p, double d)
        {
            return new Point3D(p.X / d, p.Y / d, p.Z / d);
        }

        public static Point3D operator /(double d, Point3D p)
        {
            return new Point3D(d / p.X, d / p.Y, d / p.Z);
        }

        public static Point3D operator /(Point3D p1, Point3D p2)
        {
            return new Point3D(p1.X / p2.X, p1.Y / p2.Y, p1.Z / p2.Z);
        }

        public readonly double X;
        public readonly double Y;
        public readonly double Z;
        
        public Point3D(double x, double y, double z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public Point3D(BinaryReader reader)
        {
            X = reader.ReadDouble();
            Y = reader.ReadDouble();
            Z = reader.ReadDouble();
        }
        
        public bool Equals(Point3D other)
        {
            return !ReferenceEquals(other, null) && X == other.X && Y == other.Y && Z == other.Z;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return obj is Point3D && Equals((Point3D)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = X.GetHashCode();
                hashCode = (hashCode * 397) ^ Y.GetHashCode();
                hashCode = (hashCode * 397) ^ Z.GetHashCode();
                return hashCode;
            }
        }

        public override string ToString()
        {
            return string.Format("({0:f}, {1:f}, {2:f})", X, Y, Z);
        }
    }
}
