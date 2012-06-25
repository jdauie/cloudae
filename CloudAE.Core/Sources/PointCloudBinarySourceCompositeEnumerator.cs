using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;

namespace CloudAE.Core
{
	public class PointCloudBinarySourceCompositeEnumerator : IPointCloudBinarySourceEnumerator
	{
		private readonly IPointCloudBinarySourceEnumerable[] m_sources;
		private readonly ProgressManagerProcess m_process;
		private readonly BufferInstance m_buffer;
		private readonly long m_points;

		private int m_currentSourceIndex = -1;
		private IPointCloudBinarySourceEnumerator m_currentSourceEnumerator;
		private PointCloudBinarySourceEnumeratorChunk m_current;

		public PointCloudBinarySourceCompositeEnumerator(IEnumerable<IPointCloudBinarySourceEnumerable> sources, ProgressManagerProcess process)
		{
			m_sources = sources.ToArray();
			m_process = process;
			m_buffer = m_process.AcquireBuffer(true);
			m_points = m_sources.Sum(s => s.Count);

			Reset();
		}

		public PointCloudBinarySourceEnumeratorChunk Current
		{
			get { return m_current; }
		}

		object System.Collections.IEnumerator.Current
		{
			get { return Current; }
		}

		public bool MoveNext()
		{
			// check for cancel
			if (m_current != null)
			{
				long pointsReadInPreviousSources = m_sources.Take(m_currentSourceIndex).Sum(s => s.Count);
				float progress = (pointsReadInPreviousSources + m_current.Progress * m_sources[m_currentSourceIndex].Count) / m_points;
				
				if (!m_process.Update(progress))
					return false;
			}

			if (m_currentSourceEnumerator != null && m_currentSourceEnumerator.MoveNext())
			{
				m_current = m_currentSourceEnumerator.Current;
				return true;
			}
			
			while (m_currentSourceIndex < m_sources.Length - 1)
			{
				if (m_currentSourceEnumerator != null)
					m_currentSourceEnumerator.Dispose();

				m_currentSourceIndex++;
				m_currentSourceEnumerator = m_sources[m_currentSourceIndex].GetBlockEnumerator(m_buffer);

				if (m_currentSourceEnumerator.MoveNext())
				{
					m_current = m_currentSourceEnumerator.Current;
					return true;
				}
			}

			return false;
		}

		public void Reset()
		{
			if (m_currentSourceEnumerator != null)
			{
				m_currentSourceEnumerator.Dispose();
				m_currentSourceEnumerator = null;
			}
			m_currentSourceIndex = 0;
			m_current = null;
		}

		public void Dispose()
		{
			Reset();
		}

		public IEnumerator<PointCloudBinarySourceEnumeratorChunk> GetEnumerator()
		{
			return this;
		}

		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			return this;
		}
	}
}
