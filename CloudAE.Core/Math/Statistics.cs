using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CloudAE.Core.Geometry;
using System.IO;

namespace CloudAE.Core
{
	public class Statistics : ISerializeBinary
	{
		public double Mean;
		public double StdDev;
		public double Variance;

		public Statistics(IEnumerable<float> values, float nodata)
		{
			IEnumerable<float> validValues = values.Where(v => v != nodata);
			Mean = validValues.Average();
			Variance = validValues.Average(v => Math.Pow(v - Mean, 2));
			StdDev = Math.Sqrt(Variance);
		}

		public Statistics(double mean, double variance)
		{
			Mean = mean;
			Variance = variance;
			StdDev = Math.Sqrt(Variance);
		}

		public Statistics(IEnumerable<Statistics> statsCollection)
		{
			// just average them for now
			Mean = statsCollection.Average(s => s.Mean);
			Variance = statsCollection.Average(s => s.Variance);
			StdDev = Math.Sqrt(Variance);
		}

		public Statistics(BinaryReader reader)
		{
			Mean = reader.ReadDouble();
			Variance = reader.ReadDouble();
			StdDev = Math.Sqrt(Variance);
		}

		public void Serialize(BinaryWriter writer)
		{
			writer.Write(Mean);
			writer.Write(Variance);
		}
	}

	public class StatisticsGenerator
	{
		private long m_count;
		private double? m_mean;
		private double? m_variance;

		public double Mean
		{
			get { return m_mean.Value; }
		}

		public bool HasMean
		{
			get { return m_mean.HasValue; }
		}

		public bool HasVariance
		{
			get { return m_variance.HasValue; }
		}

		public StatisticsGenerator(long sampleCount)
		{
			if (sampleCount < 1)
				throw new ArgumentOutOfRangeException("sampleCount", "The number of samples must be greater than zero.");

			m_count = sampleCount;
		}

		public void SetMean(double mean)
		{
			m_mean = mean;
		}

		public void SetVariance(double variance)
		{
			if (!HasMean)
				throw new InvalidOperationException("Variance cannot be computed without Mean.");

			if (variance < 0)
				throw new ArgumentOutOfRangeException("variance", "The sum of the samples must be non-negative.");

			m_variance = variance;
		}

		public Statistics Create()
		{
			if (!HasVariance)
				throw new InvalidOperationException("Statistics cannot be created without sample data.");

			return new Statistics(m_mean.Value, m_variance.Value);
		}
	}
}
