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
		public readonly double m_mean;
		public readonly double m_stdDev;
		public readonly double m_variance;

		public readonly double m_modeApproximate;

		#region Properties

		public double Mean
		{
			get { return m_mean; }
		}

		public double StdDev
		{
			get { return m_stdDev; }
		}

		public double Variance
		{
			get { return m_variance; }
		}

		public double ModeApproximate
		{
			get { return m_modeApproximate; }
		}

		#endregion

		public Statistics(IEnumerable<float> values, float nodata)
		{
			IEnumerable<float> validValues = values.Where(v => v != nodata);
			m_mean = validValues.Average();
			m_variance = validValues.Average(v => Math.Pow(v - Mean, 2));
			m_stdDev = Math.Sqrt(Variance);
			//m_modeApproximate
		}

		public Statistics(double mean, double variance, double mode)
		{
			m_mean = mean;
			m_variance = variance;
			m_stdDev = Math.Sqrt(Variance);
			m_modeApproximate = mode;
		}

		public Statistics(BinaryReader reader)
		{
			m_mean = reader.ReadDouble();
			m_variance = reader.ReadDouble();
			m_stdDev = Math.Sqrt(Variance);
			m_modeApproximate = reader.ReadDouble();
		}

		public void Serialize(BinaryWriter writer)
		{
			writer.Write(Mean);
			writer.Write(Variance);
			writer.Write(ModeApproximate);
		}

		public QuantizedStatistics ConvertToQuantized(UQuantization3D quantization)
		{
			uint mean = (uint)((m_mean - quantization.OffsetZ) / quantization.ScaleFactorZ);
			uint stdDev = (uint)(m_stdDev / quantization.ScaleFactorZ);
			uint variance = stdDev * stdDev;
			uint mode = (uint)((m_modeApproximate - quantization.OffsetZ) / quantization.ScaleFactorZ);

			return new QuantizedStatistics(mean, variance, mode);
		}
	}

	public class QuantizedStatistics : ISerializeBinary
	{
		public readonly uint m_mean;
		public readonly uint m_stdDev;
		public readonly uint m_variance;

		public readonly uint m_modeApproximate;

		#region Properties

		public uint Mean
		{
			get { return m_mean; }
		}

		public uint StdDev
		{
			get { return m_stdDev; }
		}

		public uint Variance
		{
			get { return m_variance; }
		}

		public uint ModeApproximate
		{
			get { return m_modeApproximate; }
		}

		#endregion

		public QuantizedStatistics(uint mean, uint variance, uint mode)
		{
			m_mean = mean;
			m_variance = variance;
			m_stdDev = (uint)Math.Sqrt(Variance);
			m_modeApproximate = mode;
		}

		public QuantizedStatistics(BinaryReader reader)
		{
			m_mean = reader.ReadUInt32();
			m_variance = reader.ReadUInt32();
			m_stdDev = (uint)Math.Sqrt(Variance);
			m_modeApproximate = reader.ReadUInt32();
		}

		public void Serialize(BinaryWriter writer)
		{
			writer.Write(Mean);
			writer.Write(Variance);
			writer.Write(ModeApproximate);
		}
	}

	public class StatisticsGenerator
	{
		private long m_count;
		private double? m_mean;
		private double? m_variance;
		private double? m_modeApprox;

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

		public void SetStatistics(double mean, double variance, double mode)
		{
			m_mean = mean;
			m_variance = variance;
			m_modeApprox = mode;
		}

		public void SetMean(double mean, double mode)
		{
			m_mean = mean;
			m_modeApprox = mode;
		}

		public void SetVariance(double variance)
		{
			if (!HasMean)
				throw new InvalidOperationException("Variance cannot be computed without Mean.");

			if (HasVariance)
				throw new InvalidOperationException("Variance has already been set.");

			if (variance < 0)
				throw new ArgumentOutOfRangeException("variance", "The sum of the samples must be non-negative.");

			m_variance = variance;
		}

		public Statistics Create()
		{
			if (!HasVariance)
				throw new InvalidOperationException("Statistics cannot be created without sample data.");

			return new Statistics(m_mean.Value, m_variance.Value, m_modeApprox.Value);
		}
	}
}
