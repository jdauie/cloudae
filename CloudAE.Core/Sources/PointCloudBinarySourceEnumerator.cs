using System;
using System.Collections.Generic;
using System.Linq;

namespace CloudAE.Core
{
	public class PointCloudBinarySourceEnumerator : IPointCloudBinarySourceEnumerator
	{
		private readonly IPointCloudBinarySourceSequentialEnumerable m_source;
		private readonly IStreamReader m_stream;
		private readonly BufferInstance m_buffer;
		private readonly ProgressManagerProcess m_process;
		private readonly long m_endPosition;
		private readonly int m_usableBytesPerBuffer;

		private PointCloudBinarySourceEnumeratorChunk m_current;

		public PointCloudBinarySourceEnumerator(IPointCloudBinarySourceSequentialEnumerable source, ProgressManagerProcess process)
		{
			m_source = source;
			m_stream = m_source.GetStreamReader();
			m_buffer = process.AcquireBuffer(true);
			m_process = process;

			m_endPosition = m_source.PointDataOffset + m_source.Count * m_source.PointSizeBytes;

			m_usableBytesPerBuffer = (m_buffer.Length / m_source.PointSizeBytes) * m_source.PointSizeBytes;

			Reset();
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="PointCloudBinarySourceEnumerator"/> class.
		/// This version does not use a process, so that it can be managed by a composite.
		/// </summary>
		/// <param name="source">The source.</param>
		/// <param name="buffer">The buffer.</param>
		public PointCloudBinarySourceEnumerator(IPointCloudBinarySourceSequentialEnumerable source, BufferInstance buffer)
		{
			m_source = source;
			m_stream = m_source.GetStreamReader();
			m_buffer = buffer;
			m_process = null;

			m_endPosition = m_source.PointDataOffset + m_source.Count * m_source.PointSizeBytes;

			m_usableBytesPerBuffer = (m_buffer.Length / m_source.PointSizeBytes) * m_source.PointSizeBytes;

			Reset();
		}

		public IPointDataProgressChunk Current
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
			if (m_current != null && m_process != null && !m_process.Update(m_current))
				return false;

			if (m_stream.Position < m_endPosition)
			{
				int bytesRead = m_stream.Read(m_buffer.Data, 0, m_usableBytesPerBuffer);

				if (bytesRead == 0)
					throw new Exception("I did something wrong");

				if (m_stream.Position > m_endPosition)
					bytesRead -= (int)(m_stream.Position - m_endPosition);

				int index = (m_current != null) ? m_current.Index + 1 : 0;
				m_current = new PointCloudBinarySourceEnumeratorChunk(index, m_buffer, bytesRead, m_source.PointSizeBytes, (float)(m_stream.Position - m_source.PointDataOffset) / (m_endPosition - m_source.PointDataOffset));

				return true;
			}

			return false;
		}

		public void Reset()
		{
			m_stream.Seek(m_source.PointDataOffset);
			m_current = null;
		}

		public void Dispose()
		{
			m_stream.Dispose();
			m_current = null;
		}

		public IEnumerator<IPointDataProgressChunk> GetEnumerator()
		{
			return this;
		}

		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			return this;
		}
	}
}
