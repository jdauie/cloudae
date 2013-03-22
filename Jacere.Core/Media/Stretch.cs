using System;
using System.Linq;

namespace Jacere.Core
{
	public abstract class StretchBase
	{
		private readonly int m_actualMin;
		private readonly int m_actualMax;

		public float StretchRatio
		{
			get { return (float)StretchRange / ActualRange; }
		}

		public uint ActualRange
		{
			get { return (uint)((long)ActualMax - ActualMin + 1); }
		}

		public uint StretchRange
		{
			get { return (uint)((long)StretchMax - StretchMin + 1); }
		}

		public int ActualMin
		{
			get { return m_actualMin; }
		}

		public int ActualMax
		{
			get { return m_actualMax; }
		}

		public abstract int StretchMin
		{
			get;
		}

		public abstract int StretchMax
		{
			get;
		}

		protected StretchBase(int actualMin, int actualMax)
		{
			m_actualMin = actualMin;
			m_actualMax = actualMax;
		}
	}

	public class StdDevStretch : StretchBase
	{
		private readonly int m_stretchMin;
		private readonly int m_stretchMax;

		private readonly QuantizedStatistics m_stats;
		private readonly float m_deviations;

		public override int StretchMin
		{
			get { return m_stretchMin; }
		}

		public override int StretchMax
		{
			get { return m_stretchMax; }
		}

		public StdDevStretch(int actualMin, int actualMax, QuantizedStatistics stats, float numDeviationsFromMean) : 
			base(actualMin, actualMax)
		{
			if (stats == null)
				throw new ArgumentNullException("stats", "StdDevStretch requires statistics.");

			m_stats = stats;
			m_deviations = numDeviationsFromMean;

			var totalDeviationFromMean = (long)(m_deviations * m_stats.StdDev);
			m_stretchMin = (int)Math.Max(ActualMin, stats.m_mean - totalDeviationFromMean);
			m_stretchMax = (int)Math.Min(ActualMax, stats.m_mean + totalDeviationFromMean);
		}
	}

	public class MinMaxStretch : StretchBase
	{
		public override int StretchMin
		{
			get { return ActualMin; }
		}

		public override int StretchMax
		{
			get { return ActualMax; }
		}

		public MinMaxStretch(int actualMin, int actualMax) : 
			base(actualMin, actualMax)
		{
		}
	}

	public class CustomStretch : StretchBase
	{
		private readonly int m_stretchMin;
		private readonly int m_stretchMax;

		public override int StretchMin
		{
			get { return m_stretchMin; }
		}

		public override int StretchMax
		{
			get { return m_stretchMax; }
		}

		public CustomStretch(int actualMin, int actualMax, int stretchMin, int stretchMax)
			: base(actualMin, actualMax)
		{
			m_stretchMin = stretchMin;
			m_stretchMax = stretchMax;
		}
	}
}
