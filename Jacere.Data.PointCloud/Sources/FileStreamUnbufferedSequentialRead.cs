using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Jacere.Data.PointCloud.Windows;
using Jacere.Data.PointCloud.Util;
using System.Diagnostics;

namespace Jacere.Data.PointCloud
{
	public class FileStreamUnbufferedSequentialRead : Stream, IStreamReader
	{
		private const FileOptions FileFlagNoBuffering = (FileOptions)0x20000000;

		private const int BUFFER_SIZE = (int)ByteSizesSmall.MB_1;

		private readonly Identity m_id;
		private readonly uint m_sectorSize;
		private readonly string m_path;

		private BufferInstance m_buffer;
		private FileStream m_stream;
		private FileStream m_streamEnd;
		private long m_streamPosition;
		private int m_bufferIndex;
		private bool m_bufferIsValid;
		private int m_bufferValidSize;

		public string Path
		{
			get { return m_path; }
		}

		public FileStreamUnbufferedSequentialRead(string path)
			: this(path, 0)
		{
		}

		public FileStreamUnbufferedSequentialRead(string path, long startPosition)
		{
			m_path = path;
			m_id = IdentityManager.AcquireIdentity(string.Format("{0}:{1}", GetType().Name, m_path));
			m_buffer = BufferManager.AcquireBuffer(m_id, true);
			m_sectorSize = PathUtil.GetDriveSectorSize(m_path);
			m_bufferValidSize = m_buffer.Length;

			const FileMode    mode    = FileMode.Open;
			const FileAccess  access  = FileAccess.Read;
			const FileShare   share   = FileShare.Read;
			const FileOptions options = (FileFlagNoBuffering | FileOptions.WriteThrough | FileOptions.SequentialScan);

			m_stream = new FileStream(m_path, mode, access, share, BUFFER_SIZE, options);
			m_streamEnd = new FileStream(m_path, mode, access, share, BUFFER_SIZE, FileOptions.WriteThrough);

			Seek(startPosition);
		}

		private long GetPositionAligned(long position)
		{
			return ((position + (m_sectorSize - 1)) & (~(long)(m_sectorSize - 1))) - m_sectorSize;
		}

		public void Seek(long position)
		{
			if (Position != position && position != 0)
			{
				long positionAligned = GetPositionAligned(position);
				m_stream.Seek(positionAligned, SeekOrigin.Begin);
				m_bufferIndex = (int)(position - positionAligned);
				m_streamPosition = positionAligned;
				m_bufferIsValid = false;
				m_bufferValidSize = m_buffer.Length;
			}
		}

		public override int Read(byte[] array, int offset, int count)
		{
			int startingOffset = offset;
			int bytesToRead = count;
			while (bytesToRead > 0)
			{
				if (!m_bufferIsValid || m_bufferIndex == m_buffer.Length)
					ReadInternal();

				// copy from array into remaining buffer
				int remainingDataInBuffer = m_bufferValidSize - m_bufferIndex;
				int bytesToCopy = Math.Min(remainingDataInBuffer, bytesToRead);

				Buffer.BlockCopy(m_buffer.Data, m_bufferIndex, array, offset, bytesToCopy);
				m_bufferIndex += bytesToCopy;
				offset += bytesToCopy;
				bytesToRead -= bytesToCopy;

				if (m_bufferValidSize < m_buffer.Length)
					break;
			}

			return (offset - startingOffset);
		}

		private void ReadInternal()
		{
			var sw = Stopwatch.StartNew();

			// a partial read is required at the end of the file
			long position = m_streamPosition;
			if (position + m_buffer.Length > m_stream.Length)
			{
				m_streamEnd.Seek(position, SeekOrigin.Begin);
				m_bufferValidSize = m_streamEnd.Read(m_buffer.Data, 0, (int)(m_streamEnd.Length - position));
			}
			else
			{
				m_stream.Read(m_buffer.Data, 0, m_buffer.Length);
				m_streamPosition += m_buffer.Length;
				m_bufferValidSize = m_buffer.Length;
			}

			// if the buffer was not valid, we just did a seek
			// and need to maintain the buffer index
			if (m_bufferIsValid)
				m_bufferIndex = 0;
			else
				m_bufferIsValid = true;

			sw.Stop();
			PerformanceManager.UpdateReadBytes(m_bufferValidSize, sw);
		}

		public void Dispose()
		{
			if (!StreamManager.IsSharedStream(this))
			{
				if (m_stream != null)
				{
					m_stream.Dispose();
					m_stream = null;
				}

				if (m_streamEnd != null)
				{
					m_streamEnd.Dispose();
					m_streamEnd = null;
				}

				if (m_buffer != null)
				{
					m_buffer.Dispose();
					m_buffer = null;
				}
			}
		}

		#region Stream Members

		public override bool CanRead
		{
			get { return true; }
		}

		public override bool CanSeek
		{
			get { return true; }
		}

		public override bool CanWrite
		{
			get { return false; }
		}

		public override void Flush()
		{
		}

		public override long Length
		{
			get { return m_stream.Length; }
		}

		public override long Position
		{
			get
			{
				// end of file
				if (m_bufferValidSize != m_buffer.Length)
					return m_streamPosition + m_bufferIndex;

				// normal
				if (m_bufferIsValid)
					return m_streamPosition - (m_buffer.Length - m_bufferIndex);

				// beginning of file
				return m_streamPosition + m_bufferIndex;
			}
			set
			{
				Seek(value);
			}
		}

		public override long Seek(long offset, SeekOrigin origin)
		{
			long actualOffset = 0;

			if (origin == SeekOrigin.Begin)
				actualOffset = offset;
			else if (origin == SeekOrigin.Current)
				actualOffset = offset + Position;
			else
				throw new ArgumentException("Unsupported SeekOrigin");

			Seek(actualOffset);

			return actualOffset;
		}

		public override void SetLength(long value)
		{
			throw new InvalidOperationException("Cannot SetLength read-only stream");
		}

		public override void Write(byte[] buffer, int offset, int count)
		{
			throw new InvalidOperationException("Cannot Write read-only stream");
		}

		#endregion
	}
}
