using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;

namespace Jacere.Core.Geometry
{
	public abstract class Quantization3D : IQuantization3D, ISerializeBinary, IEquatable<Quantization3D>
	{
		private const int LOG_ROUNDING_PRECISION = 12;

		protected readonly Point3D Offset;
		protected readonly Point3D ScaleFactor;
		protected readonly Point3D ScaleFactorInverse;

		public double OffsetX { get { return Offset.X; } }
		public double OffsetY { get { return Offset.Y; } }
		public double OffsetZ { get { return Offset.Z; } }

		public double ScaleFactorX { get { return ScaleFactor.X; } }
		public double ScaleFactorY { get { return ScaleFactor.Y; } }
		public double ScaleFactorZ { get { return ScaleFactor.Z; } }

		public double ScaleFactorInverseX { get { return ScaleFactorInverse.X; } }
		public double ScaleFactorInverseY { get { return ScaleFactorInverse.Y; } }
		public double ScaleFactorInverseZ { get { return ScaleFactorInverse.Z; } }

		protected abstract Type SupportedPointType { get; }
		protected abstract Type SupportedExtentType { get; }

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

		public void Serialize(BinaryWriter writer)
		{
			writer.Write(ScaleFactor);
			writer.Write(Offset);
		}

		public bool Equals(Quantization3D other)
		{
			return (Offset == other.Offset && ScaleFactor == other.ScaleFactor);
		}

		public static Extent3D CreateOffsetExtent(Extent3D extent)
		{
			double offsetX = Math.Floor(extent.MinX);
			double offsetY = Math.Floor(extent.MinY);
			double offsetZ = Math.Floor(extent.MinZ);

			// degrees don't like Floor
			if (extent.MinX - offsetX > extent.RangeX) offsetX = extent.MinX;
			if (extent.MinY - offsetY > extent.RangeY) offsetY = extent.MinY;
			if (extent.MinZ - offsetZ > extent.RangeZ) offsetZ = extent.MinZ;

			return new Extent3D(offsetX, offsetY, offsetZ, extent.MaxX, extent.MaxY, extent.MaxZ);
		}

        public static SQuantization3D Create(Extent3D extent)
        {
            Extent3D qOffsetExtent = CreateOffsetExtent(extent);

            // midpoint doubles the signed range,
            // but I don't really care
            var qOffsetX = qOffsetExtent.MinX;
            var qOffsetY = qOffsetExtent.MinY;
            var qOffsetZ = qOffsetExtent.MinZ;

            // Use only the non-negative int range
            double range = Math.Pow(2, 31);
            const double logBase = 10; // this value effects debugging and compressibility

            var precisionMaxX = (int)Math.Floor(Math.Log(range / (qOffsetExtent.RangeX), logBase));
            var precisionMaxY = (int)Math.Floor(Math.Log(range / (qOffsetExtent.RangeY), logBase));
            var precisionMaxZ = (int)Math.Floor(Math.Log(range / (qOffsetExtent.RangeZ), logBase));

            var qScaleFactorX = Math.Pow(logBase, -precisionMaxX);
            var qScaleFactorY = Math.Pow(logBase, -precisionMaxY);
            var qScaleFactorZ = Math.Pow(logBase, -precisionMaxZ);

            return new SQuantization3D(qScaleFactorX, qScaleFactorY, qScaleFactorZ, qOffsetX, qOffsetY, qOffsetZ);
        }
		/*
		/// <summary>
		/// This should only be called if it is not feasible to evaluate the 
		/// input data to determine what the actual scale factor should be.
		/// </summary>
        [Obsolete("Moving back to LAS compatibility", true)]
        public static Quantization3D Create(Extent3D extent, bool unsigned)
		{
			Extent3D qOffsetExtent = CreateOffsetExtent(extent);

			// midpoint doubles the signed range,
			// but I don't really care
			double qOffsetX = qOffsetExtent.MinX;
			double qOffsetY = qOffsetExtent.MinY;
			double qOffsetZ = qOffsetExtent.MinZ;

			// Use only the non-negative int range
			double range = Math.Pow(2, 31);
			const double logBase = 10; // this value effects debugging and compressibility

			int precisionMaxX = (int)Math.Floor(Math.Log(range / (qOffsetExtent.RangeX), logBase));
			int precisionMaxY = (int)Math.Floor(Math.Log(range / (qOffsetExtent.RangeY), logBase));
			int precisionMaxZ = (int)Math.Floor(Math.Log(range / (qOffsetExtent.RangeZ), logBase));

			double qScaleFactorX = Math.Pow(logBase, -precisionMaxX);
			double qScaleFactorY = Math.Pow(logBase, -precisionMaxY);
			double qScaleFactorZ = Math.Pow(logBase, -precisionMaxZ);

			if(unsigned)
				return new UQuantization3D(qScaleFactorX, qScaleFactorY, qScaleFactorZ, qOffsetX, qOffsetY, qOffsetZ);
			else
				return new SQuantization3D(qScaleFactorX, qScaleFactorY, qScaleFactorZ, qOffsetX, qOffsetY, qOffsetZ);
		}

        [Obsolete("Moving back to LAS compatibility", true)]
		public static UQuantization3D Create(Extent3D extent, SQuantization3D inputQuantization, int[][] testValues, int count)
		{
			// determine best scale factors
			double[] scaleFactors = new double[] { inputQuantization.ScaleFactorX, inputQuantization.ScaleFactorY, inputQuantization.ScaleFactorZ };
			for (int i = 0; i < 3; i++)
			{
				int[] values = testValues[i];
				values.ParallelSort(count);
				//Array.Sort<int>(values);

				// determine the base of the scale factor
				int scaleInverse = (int)Math.Ceiling(1 / scaleFactors[i]);
				int scaleBase = FindBase(scaleInverse);
				int scalePow = (int)Math.Round(Math.Log(scaleInverse, scaleBase), LOG_ROUNDING_PRECISION);

				// count differences
				var diffCountsLookup = new Dictionary<uint, int>();
				for (int p = 1; p < count; p++)
				{
					uint diff = (uint)(values[p] - values[p - 1]);
					if (diffCountsLookup.ContainsKey(diff))
						++diffCountsLookup[diff];
					else
						diffCountsLookup.Add(diff, 1);
				}

				uint[] diffs = diffCountsLookup.Select(kvp => kvp.Key).Where(k => k > 0).ToArray();
				int diffsLength = diffs.Length;
				int[] diffCounts = new int[diffsLength];
				for (int d = 0; d < diffs.Length; d++)
					diffCounts[d] = diffCountsLookup[diffs[d]];

				int differenceCount = diffs.Length;
				double[] diffPow = new double[differenceCount];
				for (int d = 0; d < differenceCount; d++)
					diffPow[d] = Math.Log(diffs[d], scaleBase);

				int nonZeroDiffPointCount = diffCounts.Sum();
				double[] diffPowComponentRatio = new double[differenceCount];
				for (int d = 0; d < differenceCount; d++)
					diffPowComponentRatio[d] = diffPow[d] * diffCounts[d] / nonZeroDiffPointCount;

				// this rounding is a WAG
				double componentSum = Math.Round(diffPowComponentRatio.Sum(), 3);
				int componentSumPow = (int)componentSum;
#warning this is a bit sketchy -- look at this again, with a range of data
				if (componentSumPow < scalePow && componentSumPow > 0)
					scaleFactors[i] = Math.Pow(scaleBase, componentSumPow - scalePow);
			}

			if (scaleFactors[0] != scaleFactors[1])
				throw new Exception("The X and Y scale factors should be the same. X = {0}, Y = {1}");

			Extent3D offsetExtent = CreateOffsetExtent(extent);

			return new UQuantization3D(scaleFactors[0], scaleFactors[1], scaleFactors[2], offsetExtent.MinX, offsetExtent.MinY, offsetExtent.MinZ);
		}

        [Obsolete("Moving back to LAS compatibility", true)]
		public static UQuantization3D Create(Extent3D extent, double[][] testValues, int count)
		{
			// determine best scale factors
			double[] scaleFactors = new double[3];
			for (int i = 0; i < 3; i++)
			{
				double[] values = testValues[i];
				Array.Sort(values, 0, count);

				const int scaleBase = 10;

				// count differences
				var diffCounts = new SortedList<double, int>();

				for (int p = 1; p < count; p++)
				{
					double diff = values[p] - values[p - 1];
					if (diffCounts.ContainsKey(diff))
						++diffCounts[diff];
					else
						diffCounts.Add(diff, 1);
				}

				int differenceCount = diffCounts.Count;
				double[] diffPow = new double[differenceCount];
				for (int d = 1; d < differenceCount; d++)
					diffPow[d] = Math.Log(diffCounts.Keys[d], scaleBase);

				int nonZeroDiffPointCount = diffCounts.SkipWhile(kvp => kvp.Key == 0).Sum(kvp => kvp.Value);
				double[] diffPowComponentRatio = new double[differenceCount];
				for (int d = 1; d < differenceCount; d++)
					diffPowComponentRatio[d] = diffPow[d] * diffCounts.Values[d] / nonZeroDiffPointCount;

				// this rounding is a WAG
#warning I had to change rounding from 4 digits to 1, evaluate this
				double componentSum = Math.Round(diffPowComponentRatio.Sum(), 1);
				int componentSumPow = (int)componentSum;
				scaleFactors[i] = Math.Pow(scaleBase, componentSumPow);
			}

			if (scaleFactors[0] != scaleFactors[1])
				throw new Exception("The X and Y scale factors should be the same. X = {0}, Y = {1}");

			Extent3D offsetExtent = CreateOffsetExtent(extent);

			return new UQuantization3D(scaleFactors[0], scaleFactors[1], scaleFactors[2], offsetExtent.MinX, offsetExtent.MinY, offsetExtent.MinZ);
		}
		*/
		private static int FindBase(int inverseScale)
		{
			// find factors
			var factors = new Dictionary<int, int>();

			int currentFactorValue = 2;

			int remainder = inverseScale;
			while (remainder > 1)
			{
				if (remainder % currentFactorValue == 0)
				{
					remainder = remainder / currentFactorValue;
					if (factors.ContainsKey(currentFactorValue))
						++factors[currentFactorValue];
					else
						factors.Add(currentFactorValue, 1);
				}
				else
				{
					++currentFactorValue;
				}
			}

			int smallestCount = factors.Values.Min();

			int scaleBase = 1;
			foreach (int factor in factors.Keys)
				scaleBase *= (factor * (factors[factor] / smallestCount));

			return scaleBase;
		}

		public Point3D Convert(IQuantizedPoint3D point)
		{
			if (point.GetType() != SupportedPointType)
				throw new ArgumentException("Quantization type mismatch", "point");

			return point.GetPoint3D() * ScaleFactor + Offset;
		}

		public Extent3D Convert(IQuantizedExtent3D extent)
		{
			if (extent.GetType() != SupportedExtentType)
				throw new ArgumentException("Quantization type mismatch", "extent");

			var e = extent.GetExtent3D();
			return new Extent3D(
				e.GetMinPoint3D() * ScaleFactor + Offset,
				e.GetMaxPoint3D() * ScaleFactor + Offset
			);
		}
	}
}
