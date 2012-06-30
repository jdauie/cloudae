using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;

namespace CloudAE.Core
{
	public class PointCloudBinarySourceEnumerator : IPointCloudBinarySourceEnumerator
	{
		private readonly IPointCloudBinarySourceEnumerable m_source;
		private readonly IStreamReader m_stream;
		private readonly BufferInstance m_buffer;
		private readonly ProgressManagerProcess m_process;
		private readonly long m_endPosition;
		private readonly int m_usableBytesPerBuffer;

		private PointCloudBinarySourceEnumeratorChunk m_current;

		public PointCloudBinarySourceEnumerator(IPointCloudBinarySourceEnumerable source, ProgressManagerProcess process)
		{
			m_source = source;
			m_stream = new FileStreamUnbufferedSequentialRead(m_source.FilePath);
			m_buffer = process.AcquireBuffer(true);
			m_process = process;

			m_endPosition = m_source.PointDataOffset + m_source.Count * m_source.PointSizeBytes;

			m_usableBytesPerBuffer = m_source.UsableBytesPerBuffer;

			Reset();
		}

		public PointCloudBinarySourceEnumerator(IPointCloudBinarySourceEnumerable source, BufferInstance buffer)
		{
			m_source = source;
			m_stream = new FileStreamUnbufferedSequentialRead(m_source.FilePath);
			m_buffer = buffer;
			m_process = null;

			m_endPosition = m_source.PointDataOffset + m_source.Count * m_source.PointSizeBytes;

			m_usableBytesPerBuffer = m_source.UsableBytesPerBuffer;

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
			if (m_current != null && m_process != null && !m_process.Update(m_current))
				return false;

			if (m_stream.Position < m_endPosition)
			{
				int bytesRead = m_stream.Read(m_buffer.Data, 0, m_usableBytesPerBuffer);

				if (m_stream.Position > m_endPosition)
					bytesRead -= (int)(m_stream.Position - m_endPosition);

				int pointsRead = bytesRead / m_source.PointSizeBytes;

				uint index = (m_current != null) ? m_current.Index + 1 : 0;
				m_current = new PointCloudBinarySourceEnumeratorChunk(index, m_buffer, bytesRead, pointsRead, (float)(m_stream.Position - m_source.PointDataOffset) / (m_endPosition - m_source.PointDataOffset));

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
