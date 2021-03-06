﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;

using Jacere.Core.Geometry;

namespace Jacere.Core
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

        public QuantizedStatistics ConvertToQuantized(SQuantization3D quantization)
		{
			var mean = (int)((m_mean - quantization.OffsetZ) / quantization.ScaleFactorZ);
			var stdDev = (uint)(m_stdDev / quantization.ScaleFactorZ);
			var mode = (int)((m_modeApproximate - quantization.OffsetZ) / quantization.ScaleFactorZ);

			return new QuantizedStatistics(mean, stdDev, mode);
		}
	}

	public class QuantizedStatistics : ISerializeBinary
	{
		public readonly int m_mean;
		public readonly uint m_stdDev;
		public readonly ulong m_variance;

		public readonly int m_modeApproximate;

		#region Properties

		public int Mean
		{
			get { return m_mean; }
		}

		public uint StdDev
		{
			get { return m_stdDev; }
		}

		public ulong Variance
		{
			get { return m_variance; }
		}

		public int ModeApproximate
		{
			get { return m_modeApproximate; }
		}

		#endregion

		public QuantizedStatistics(int mean, uint stdDev, int mode)
		{
			m_mean = mean;
			m_stdDev = stdDev;
			m_variance = (ulong)Math.Pow(m_stdDev, 2);
			m_modeApproximate = mode;
		}

		public QuantizedStatistics(BinaryReader reader)
		{
			m_mean = reader.ReadInt32();
			m_stdDev = reader.ReadUInt32();
			m_variance = (ulong)Math.Pow(Variance, 2);
			m_modeApproximate = reader.ReadInt32();
		}

		public void Serialize(BinaryWriter writer)
		{
			writer.Write(Mean);
			writer.Write(StdDev);
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
