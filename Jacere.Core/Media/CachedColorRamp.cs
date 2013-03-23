using System;
using System.Collections.Generic;
using System.Linq;

namespace Jacere.Core
{
	/// <summary>
	/// Pre-calculated color ramp using the 2^n mapping mechanism.
	/// Stretched for a specified size and input range (including std dev stretch).
	/// </summary>
	public class CachedColorRamp
	{
		private readonly ColorRamp m_ramp;
		private readonly IntervalMap m_map;

		private readonly int[] m_bins;

		public int GetColor(int z)
		{
			return m_bins[m_map.GetInterval(z)];
		}

		public CachedColorRamp(ColorRamp ramp, StretchBase stretch, int desiredDestinationBins)
		{
			if (stretch == null)
				throw new ArgumentNullException("stretch");

			m_ramp = ramp;
			m_map = new IntervalMap(stretch, desiredDestinationBins, false);

			// allow overflow
			m_bins = new int[m_map.Count + 1];

			foreach (var interval in m_map.GetIntervals())
				m_bins[interval.Index] = m_ramp.GetColor(interval.StretchRatio).ToArgb();

			// overflow
			m_bins[m_bins.Length - 1] = m_bins[m_bins.Length - 2];
		}
	}
}
