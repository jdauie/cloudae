using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace CloudAE.Core.Geometry
{
	/// <summary>
	/// Immutable extent class.
	/// </summary>
	public class Extent2D : ISerializeBinary
	{
		private const double ERROR_BOUND = 0.005;

		public readonly double MinX;
		public readonly double MinY;

		public readonly double MaxX;
		public readonly double MaxY;

		public Extent2D(double minX, double minY, double maxX, double maxY)
		{
			MinX = minX;
			MinY = minY;
			MaxX = maxX;
			MaxY = maxY;
		}

		public Extent2D(params IPoint2D[] points)
		{
			if (points.Length == 0)
				throw new ArgumentException("There must be at least one point to compute extent.", "points");

			double minX = 0, minY = 0;
			double maxX = 0, maxY = 0;

			bool initialized = false;

			for (int i = 0; i < points.Length; i++)
			{
				IPoint2D point = points[i];
				double x = point.X;
				double y = point.Y;

				if (!initialized)
				{
					minX = maxX = x;
					minY = maxY = y;

					initialized = true;
				}
				else
				{
					if (x < minX) minX = x; else if (x > maxX) maxX = x;
					if (y < minY) minY = y; else if (y > maxY) maxY = y;
				}
			}

			MinX = minX;
			MaxX = maxX;
			MinY = minY;
			MaxY = maxY;
		}

		public Extent2D(BinaryReader reader)
		{
			MinX = reader.ReadDouble();
			MaxX = reader.ReadDouble();
			MinY = reader.ReadDouble();
			MaxY = reader.ReadDouble();
		}

		public virtual void Serialize(BinaryWriter writer)
		{
			writer.Write(MinX);
			writer.Write(MaxX);
			writer.Write(MinY);
			writer.Write(MaxY);
		}

		public double RangeX
		{
			get { return MaxX - MinX; }
		}

		public double RangeY
		{
			get { return MaxY - MinY; }
		}

		public double MidpointX
		{
			get { return (MaxX + MinX) / 2; }
		}

		public double MidpointY
		{
			get { return (MaxY + MinY) / 2; }
		}

		public double Area
		{
			get { return RangeX * RangeY; }
		}

		public double Aspect
		{
			get { return RangeX / RangeY; }
		}

		public bool Contains(Extent2D extent)
		{
			return (extent.MinX >= MinX && extent.MaxX <= MaxX && extent.MinY >= MinY && extent.MaxY <= MaxY);
		}

		public bool Contains(double x, double y)
		{
			return (x >= MinX && x <= MaxX && y >= MinY && y <= MaxY);
		}

		public bool Contains(double x, double y, bool error_bound)
		{
			double eb = (error_bound) ? ERROR_BOUND : 0.0f;

			// FUTURE: Implement comparison operator for float/double equals... the below may work
			//         in practice but in general it is unsafe due to mantisa/exponent ratios.
			return ((MinX - eb) <= x) && (x <= (MaxX + eb)) && ((MinY - eb) <= y) && (y <= (MaxY + eb));
		}

		/// <summary>
		/// Returns a <see cref="System.String"/> that represents this instance.
		/// </summary>
		/// <returns>
		/// A <see cref="System.String"/> that represents this instance.
		/// </returns>
		public override string ToString()
		{
			return String.Format("({0:f}, {1:f})", RangeX, RangeY);
		}
	}
}
