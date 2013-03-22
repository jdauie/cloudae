using System;
using System.Collections.Generic;
using System.Linq;

namespace Jacere.Core
{
	public struct IntervalMapIndex
	{
		public readonly int Index;
		public readonly float StretchRatio;

		public IntervalMapIndex(int index, float ratio)
		{
			Index = index;
			StretchRatio = ratio;
		}
	}

	/// <summary>
	/// For signed quantized ranges
	/// </summary>
	public class IntervalMap
	{
		private readonly StretchBase m_stretch;
		private readonly bool m_scaleDesiredBinsToStretchRange;

		private readonly int m_binCountDesired;
		private readonly int m_binCountEstimated;
		private readonly int m_binCountPow;
		private readonly int m_binCount;
		
		private readonly int m_actualRangePow;
		private readonly uint m_actualRange;

		private readonly int m_rightShift;

		private readonly int m_actualMinShifted;
		private readonly int m_actualMaxShifted;

		private readonly int m_stretchMinShifted;
		private readonly int m_stretchMaxShifted;

		public int Count
		{
			get { return m_binCount; }
		}

		public IntervalMap(StretchBase stretch, int desiredDestinationBins, bool scaleDesiredBinsToStretchRange)
		{
			m_scaleDesiredBinsToStretchRange = scaleDesiredBinsToStretchRange;

			m_stretch = stretch;
			m_binCountDesired = desiredDestinationBins;
			m_binCountEstimated = m_binCountDesired;

			// determine how many total bins are required if the 
			// desired bin count is used for the stretch range
			if (m_scaleDesiredBinsToStretchRange)
				m_binCountEstimated = (int)(m_binCountEstimated / m_stretch.StretchRatio);

			m_actualRangePow = (int)Math.Ceiling(Math.Log(m_stretch.ActualRange, 2));
			m_actualRange = (uint)Math.Pow(2, m_actualRangePow);

			// handle max range overflow (unlikely)
			if (m_actualRangePow == 32)
				m_actualRange = uint.MaxValue;

			m_binCountPow = (int)Math.Ceiling(Math.Log(m_binCountEstimated, 2));

			// make sure that there are not more bins than discrete values
			if (m_binCountPow > m_actualRangePow)
				m_binCountPow = m_actualRangePow;

			m_binCount = (int)Math.Pow(2, m_binCountPow);

			m_rightShift = (m_actualRangePow - m_binCountPow);

			m_actualMinShifted = m_stretch.ActualMin >> m_rightShift;
			m_actualMaxShifted = m_stretch.ActualMax >> m_rightShift;

			m_stretchMinShifted = m_stretch.StretchMin >> m_rightShift;
			m_stretchMaxShifted = m_stretch.StretchMax >> m_rightShift;
		}

		public int GetInterval(int actualValue)
		{
			return (actualValue >> m_rightShift) - m_actualMinShifted;
		}

		public IEnumerable<IntervalMapIndex> GetIntervals()
		{
			// pre
			foreach (var i in Enumerable.Range(m_actualMinShifted, m_stretchMinShifted - m_actualMinShifted))
				yield return new IntervalMapIndex(i - m_actualMinShifted, 0.0f);

			// stretch
			foreach (var i in Enumerable.Range(m_stretchMinShifted, m_stretchMaxShifted - m_stretchMinShifted + 1))
				yield return new IntervalMapIndex(i - m_actualMinShifted, (float)(i - m_stretchMinShifted) / (m_stretchMaxShifted - m_stretchMinShifted));

			// post
			foreach (var i in Enumerable.Range(m_stretchMaxShifted + 1, m_actualMaxShifted - m_stretchMaxShifted))
				yield return new IntervalMapIndex(i - m_actualMinShifted, 1.0f);
		}
	}
}
