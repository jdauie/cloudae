using System;
using System.Collections.Generic;
using System.Linq;

using Jacere.Data.PointCloud;

namespace CloudAE.Core
{
	// in order for this to make sense, the chunk index needs to map back to the input
	// it would be nice if I could get sector-aligned numbers, but that's too much for now
	public class GridIndexCell
	{
		private readonly HashSet<int> m_chunks;

		public IEnumerable<int> Chunks
		{
			get { return m_chunks; }
		}

		public bool HasChunks
		{
			get { return m_chunks.Count > 0; }
		}

		public GridIndexCell()
		{
			m_chunks = new HashSet<int>();
		}

		public GridIndexCell(GridIndexCell a, GridIndexCell b)
		{
			m_chunks = new HashSet<int>();

			if (a != null)
				foreach (var c in a.m_chunks)
					m_chunks.Add(c);

			if (b != null)
				foreach (var c in b.m_chunks)
					m_chunks.Add(c);
		}

		public void Add(int chunkIndex)
		{
			m_chunks.Add(chunkIndex);
		}

		public override string ToString()
		{
			// debugging only
			return string.Format("{0}", string.Join(",", m_chunks));
		}
	}

	public class PointCloudBinarySourceEnumeratorSparseGridRegion : PointCloudBinarySourceEnumeratorSparseRegion
	{
		private readonly GridRange m_range;

		public GridRange GridRange
		{
			get { return m_range; }
		}

		public PointCloudBinarySourceEnumeratorSparseGridRegion(IEnumerable<Range> regions, GridRange range, int maxPointCountPerChunk)
			: base(regions, maxPointCountPerChunk)
		{
			m_range = range;
		}
	}
}
