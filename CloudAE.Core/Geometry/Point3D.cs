using System;
using System.IO;
using System.Linq;

namespace CloudAE.Core.Geometry
{
	/// <summary>
	/// Immutable point class.
	/// </summary>
	public struct Point3D : IPoint3D, ISerializeBinary
	{
		#region Operators

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

		#endregion

		private readonly double m_x;
		private readonly double m_y;
		private readonly double m_z;

		public double X
		{
			get { return m_x; }
		}

		public double Y
		{
			get { return m_y; }
		}

		public double Z
		{
			get { return m_z; }
		}

		public Point3D(double x, double y, double z)
		{
			m_x = x;
			m_y = y;
			m_z = z;
		}

		public Point3D(BinaryReader reader)
		{
			m_x = reader.ReadDouble();
			m_y = reader.ReadDouble();
			m_z = reader.ReadDouble();
		}

		public void Serialize(BinaryWriter writer)
		{
			writer.Write(X);
			writer.Write(Y);
			writer.Write(Z);
		}

		/// <summary>
		/// Returns a <see cref="System.String"/> that represents this instance.
		/// </summary>
		/// <returns>
		/// A <see cref="System.String"/> that represents this instance.
		/// </returns>
		public override string ToString()
		{
			return String.Format("({0:f}, {1:f}, {2:f})", X, Y, Z);
		}
	}
}
