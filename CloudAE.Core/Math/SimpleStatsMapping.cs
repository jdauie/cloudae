using System;
using System.Collections.Generic;
using System.Linq;
using CloudAE.Core.Geometry;

namespace CloudAE.Core
{
	public class SimpleStatisticsMapping : IChunkProcess
	{
		private readonly int m_intervals;
		private readonly long[] m_counts;
		private readonly float m_intervalsOverRangeZ;

		private readonly double m_min;
		private readonly double m_range;


		public SimpleStatisticsMapping(double min, double range, int intervals)
		{
			m_intervals = intervals;
			m_min = min;
			m_range = range;
			m_counts = new long[m_intervals + 1];
			m_intervalsOverRangeZ = (float)(m_intervals / range);
		}

		public unsafe IPointDataChunk Process(IPointDataChunk chunk)
		{
			byte* pb = chunk.PointDataPtr;
			while (pb < chunk.PointDataEndPtr)
			{
				Point3D* p = (Point3D*)pb;
				++m_counts[(int)(((*p).Z - m_min) * m_intervalsOverRangeZ)];
				pb += chunk.PointSizeBytes;
			}

			return chunk;
		}

		public Statistics ComputeStatistics()
		{
			return ScaledStatisticsMapping.ComputeStatistics(m_counts, true, m_min, m_range);
		}
	}
}
