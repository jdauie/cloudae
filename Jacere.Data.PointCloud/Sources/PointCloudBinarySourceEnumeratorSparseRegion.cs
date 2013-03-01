using System;
using System.Collections.Generic;
using System.Linq;

namespace Jacere.Data.PointCloud
{
	public class Range
	{
		private readonly int m_index;
		private readonly int m_count;

		public int Index
		{
			get { return m_index; }
		}

		public int Count
		{
			get { return m_count; }
		}

		public Range(int index, int count)
		{
			m_index = index;
			m_count = count;
		}
	}

	public class PointCloudBinarySourceEnumeratorSparseRegion : IEnumerable<PointCloudBinarySourceEnumeratorRegion>
	{
		private readonly List<PointCloudBinarySourceEnumeratorRegion> m_regions;
		private readonly int m_chunkCount;
		private readonly int m_pointsPerChunk;

		public int PointsPerChunk
		{
			get { return m_pointsPerChunk; }
		}

		public PointCloudBinarySourceEnumeratorSparseRegion(IEnumerable<Range> regions, int maxPointCountPerChunk)
		{
			m_pointsPerChunk = maxPointCountPerChunk;
			m_regions = new List<PointCloudBinarySourceEnumeratorRegion>();
			foreach (var region in regions)
			{
				var r = new PointCloudBinarySourceEnumeratorRegion(region.Index, region.Count);
				m_chunkCount += r.ChunkCount;
				m_regions.Add(r);
			}
		}

		public override string ToString()
		{
			return string.Format("{0} ({1})", m_regions.Count, m_chunkCount);
		}

		#region IEnumerable Members

		public IEnumerator<PointCloudBinarySourceEnumeratorRegion> GetEnumerator()
		{
			return m_regions.GetEnumerator();
		}

		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		#endregion
	}
}
