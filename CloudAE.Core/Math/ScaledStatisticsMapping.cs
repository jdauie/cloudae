using System;
using System.Linq;
using CloudAE.Core.Geometry;

namespace CloudAE.Core
{
	public class ScaledStatisticsMapping
	{
		private readonly int m_sourceMin;
		private readonly uint m_sourceRange;
		private readonly int m_sourceRangeExtendedPow;
		private readonly uint m_sourceRangeExtended;

		private readonly int m_binCountDesired;
		private readonly int m_binCountPow;
		private readonly int m_binCount;

		private readonly int m_sourceMinShifted;
		private readonly int m_sourceRightShift;

		private readonly long[] m_bins;

		public long[] DestinationBins
		{
			get { return m_bins; }
		}

		public int SourceRightShift
		{
			get { return m_sourceRightShift; }
		}

		public int SourceMinShifted
		{
			get { return m_sourceMinShifted; }
		}

		public ScaledStatisticsMapping(int sourceMin, uint sourceRange, int desiredDestinationBins)
		{
			m_sourceMin = sourceMin;
			m_sourceRange = sourceRange;
			m_binCountDesired = desiredDestinationBins;

			// extend range for destination
			m_binCountPow = (int)Math.Ceiling(Math.Log(m_binCountDesired, 2));
			m_binCount = (int)Math.Pow(2, m_binCountPow);

			// extend range for source
			m_sourceRangeExtendedPow = (int)Math.Ceiling(Math.Log(m_sourceRange, 2));
			m_sourceRangeExtended = (uint)Math.Pow(2, m_sourceRangeExtendedPow);
			if (m_sourceRangeExtendedPow == 32)
				m_sourceRangeExtended = uint.MaxValue;
			else if (m_sourceRangeExtendedPow > 32)
				throw new Exception("how is this possible");

			// assume only right shifts will be required
			if (m_binCountPow > m_sourceRangeExtendedPow)
				throw new Exception("I did not expect this");

			m_sourceRightShift = m_sourceRangeExtendedPow - m_binCountPow;
			m_sourceMinShifted = m_sourceMin >> m_sourceRightShift;

			m_bins = new long[m_binCount + 1];
		}

		private long[] Finalize()
		{
			// find the highest bin that would map to a value within the source range
			// this will result in a *slight* shift of the mapping, but it is simpler
			int highestValidBin = (int)((double)m_sourceRange / m_sourceRangeExtended * m_binCount);

			// correct overflow
			m_bins[highestValidBin] += m_bins[highestValidBin + 1];
			m_bins[highestValidBin + 1] = 0;

			long[] validBins = new long[highestValidBin + 1];
			Array.Copy(m_bins, validBins, validBins.Length);

			return validBins;
		}

		public unsafe void Process(IPointDataChunk chunk)
		{
			byte* pb = chunk.PointDataPtr;
			while (pb < chunk.PointDataEndPtr)
			{
				SQuantizedPoint3D* p = (SQuantizedPoint3D*)pb;
				++m_bins[((*p).Z >> SourceRightShift) - SourceMinShifted];
				pb += chunk.PointSizeBytes;
			}
		}

		public Statistics ComputeStatistics(double destinationMin, double destinationRange)
		{
			long[] verticalValueCounts = Finalize();
			return ComputeStatistics(verticalValueCounts, false, destinationMin, destinationRange);
		}

		public static Statistics ComputeStatistics(long[] verticalValueCounts, bool overflow, double destinationMin, double destinationRange)
		{
			int verticalValueIntervals = verticalValueCounts.Length;

			if (overflow)
			{
				--verticalValueIntervals;
				verticalValueCounts[verticalValueIntervals - 1] += verticalValueCounts[verticalValueIntervals];
				verticalValueCounts[verticalValueIntervals] = 0;
			}

			long count = verticalValueCounts.Sum();

			double[] verticalValueCenters = new double[verticalValueIntervals];
			for (int i = 0; i < verticalValueCenters.Length; i++)
				verticalValueCenters[i] = (i + 0.5) / verticalValueIntervals * destinationRange + destinationMin;

			double mean = 0;
			for (int i = 0; i < verticalValueCenters.Length; i++)
				mean += (double)verticalValueCounts[i] / count * verticalValueCenters[i];

			double variance = 0;
			for (int i = 0; i < verticalValueIntervals; i++)
				variance += verticalValueCounts[i] * Math.Pow(verticalValueCenters[i] - mean, 2);

			variance /= (count - 1);

			int intervalMax = verticalValueCounts.MaxIndex();
			double mode = verticalValueCenters[intervalMax];

			return new Statistics(mean, variance, mode);
		}
	}
}
