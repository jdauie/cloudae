using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Jacere.Data.PointCloud
{
	public class PointCloudBinarySourceEnumeratorRegion
	{
		public readonly int ChunkStart;
		public readonly int ChunkCount;

		public PointCloudBinarySourceEnumeratorRegion(int chunkStart, int chunkCount)
		{
			ChunkStart = chunkStart;
			ChunkCount = chunkCount;
		}

		public override string ToString()
		{
			return string.Format("[{0}-{1}] ({2})", ChunkStart, ChunkStart + ChunkCount, ChunkCount);
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

		public PointCloudBinarySourceEnumeratorSparseRegion(IEnumerable<KeyValuePair<int, int>> regions, int maxPointCountPerChunk)
		{
			m_pointsPerChunk = maxPointCountPerChunk;
			m_regions = new List<PointCloudBinarySourceEnumeratorRegion>();
			foreach (var region in regions)
			{
				var r = new PointCloudBinarySourceEnumeratorRegion(region.Key, region.Value);
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
