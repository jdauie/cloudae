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

	public class GridIndexSegments : IEnumerable<PointCloudBinarySourceEnumeratorSparseRegion>
	{
		private readonly List<PointCloudBinarySourceEnumeratorSparseRegion> m_regionSourcesBySegment;
		private readonly int[] m_tilesPerSegment;

		public int GetSegmentTileRange(PointCloudBinarySourceEnumeratorSparseRegion region)
		{
			int index = m_regionSourcesBySegment.IndexOf(region);
			if (index == -1)
				throw new Exception("Region does not belong");

			return m_tilesPerSegment[index];
		}

		public GridIndexSegments(IEnumerable<SortedDictionary<int, int>> regionSourcesBySegment, List<int> tilesPerSegment, int maxPointCountPerChunk)
		{
			m_regionSourcesBySegment = new List<PointCloudBinarySourceEnumeratorSparseRegion>();
			foreach (var segment in regionSourcesBySegment)
				m_regionSourcesBySegment.Add(new PointCloudBinarySourceEnumeratorSparseRegion(segment, maxPointCountPerChunk));
			m_tilesPerSegment = tilesPerSegment.ToArray();

			if (m_regionSourcesBySegment.Count != m_tilesPerSegment.Length)
				throw new Exception("GridIndex Tile/Segment mismatch");
		}

		#region IEnumerable Members

		public IEnumerator<PointCloudBinarySourceEnumeratorSparseRegion> GetEnumerator()
		{
			return m_regionSourcesBySegment.GetEnumerator();
		}

		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		#endregion
	}
}
