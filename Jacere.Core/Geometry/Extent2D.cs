using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;

namespace Jacere.Core.Geometry
{
	public interface IAspect
	{
		double Aspect { get; }
	}

	/// <summary>
	/// Immutable extent class.
	/// </summary>
	public class Extent2D : ISerializeBinary, IAspect
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

			var initialized = false;

			foreach (var point in points)
			{
				var x = point.X;
				var y = point.Y;

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
			MaxX = reader.ReadDouble();
			MinX = reader.ReadDouble();
			MaxY = reader.ReadDouble();
			MinY = reader.ReadDouble();
		}

		public virtual void Serialize(BinaryWriter writer)
		{
			// this ordering conforms to the LAS header
			writer.Write(MaxX);
			writer.Write(MinX);
			writer.Write(MaxY);
			writer.Write(MinY);
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

		public Grid<T> CreateGridFromDimension<T>(ushort maxDimension, bool buffered = false, T fillVal = default(T))
		{
			var sizeX = maxDimension;
			var sizeY = maxDimension;

			var aspect = Aspect;
			if (aspect > 1)
				sizeY = (ushort)Math.Ceiling(sizeY / aspect);
			else
				sizeX = (ushort)Math.Ceiling(sizeX * aspect);

			return Grid<T>.Create(sizeX, sizeY, buffered, fillVal);
		}

		public Grid<T> CreateGridFromCellSize<T>(double cellSize, bool buffered = false, T fillVal = default(T))
		{
			var sizeX = (ushort)Math.Ceiling(RangeX / cellSize);
			var sizeY = (ushort)Math.Ceiling(RangeY / cellSize);

			return Grid<T>.Create(sizeX, sizeY, buffered, fillVal);
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
