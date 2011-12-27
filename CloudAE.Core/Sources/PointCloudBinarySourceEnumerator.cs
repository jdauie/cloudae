﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace CloudAE.Core
{
	public class PointCloudBinarySourceEnumerator : IEnumerator<PointCloudBinarySourceEnumeratorChunk>, IEnumerable<PointCloudBinarySourceEnumeratorChunk>
	{
		PointCloudBinarySource m_source;
		private FileStream m_stream;
		private byte[] m_buffer;
		private long m_endPosition;
		private int m_currentBytesRead;

		public PointCloudBinarySourceEnumerator(PointCloudBinarySource source, byte[] buffer)
		{
			m_source = source;
			m_buffer = buffer;
			m_stream = new FileStream(m_source.FilePath, FileMode.Open, FileAccess.Read, FileShare.None, BufferManager.BUFFER_SIZE_BYTES, FileOptions.SequentialScan);

			m_endPosition = m_source.PointDataOffset + (long)m_source.Count * m_source.PointSizeBytes;

			Reset();
		}

		public PointCloudBinarySourceEnumeratorChunk Current
		{
			get { return new PointCloudBinarySourceEnumeratorChunk(m_currentBytesRead, (float)(m_stream.Position - m_source.PointDataOffset) / (m_endPosition - m_source.PointDataOffset)); }
		}

		object System.Collections.IEnumerator.Current
		{
			get { return Current; }
		}

		public bool MoveNext()
		{
			if (m_stream.Position < m_endPosition)
			{
				int bytesRead = m_stream.Read(m_buffer, 0, m_source.UsableBytesPerBuffer);

				if (m_stream.Position > m_endPosition)
					bytesRead -= (int)(m_stream.Position - m_endPosition);

				m_currentBytesRead = bytesRead;

				return true;
			}

			return false;
		}

		public void Reset()
		{
			m_stream.Seek(m_source.PointDataOffset, SeekOrigin.Begin);
			m_currentBytesRead = 0;
		}

		public void Dispose()
		{
			m_stream.Dispose();
			m_stream = null;
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
