using System;
using System.IO;
using System.Linq;
using CloudAE.Core.Util;
using System.Diagnostics;

namespace CloudAE.Core
{
	public class FileStreamUnbufferedSequentialWrite : Stream, IStreamWriter
	{
		private const FileOptions FileFlagNoBuffering = (FileOptions)0x20000000;

		private const int BUFFER_SIZE = (int)ByteSizesSmall.MB_1;

		private readonly Identity m_id;
		private readonly uint m_sectorSize;
		private readonly string m_path;
		private readonly long m_length;
		private readonly long m_lengthAligned;
		private readonly bool m_truncateOnClose;

		private BufferInstance m_buffer;
		private FileStream m_stream;
		private int m_bufferIndex;
		private long m_actualLength;

		public FileStreamUnbufferedSequentialWrite(string path, long length, long startPosition)
			: this(path, length, startPosition, false)
		{
		}

		public FileStreamUnbufferedSequentialWrite(string path, long length, long startPosition, bool truncateOnClose)
		{
			m_path = path;
			m_id = IdentityManager.AcquireIdentity(string.Format("{0}:{1}", this.GetType().Name, m_path));
			m_buffer = BufferManager.AcquireBuffer(m_id, true);
			m_sectorSize = PathUtil.GetDriveSectorSize(m_path);

			m_length = length;
			m_lengthAligned = (m_length + (m_sectorSize - 1)) & (~(long)(m_sectorSize - 1));
			m_truncateOnClose = truncateOnClose;

			const FileMode mode = FileMode.OpenOrCreate;
			const FileAccess access = FileAccess.Write;
			const FileShare share = FileShare.None;
			const FileOptions options = (FileFlagNoBuffering | FileOptions.WriteThrough | FileOptions.SequentialScan);

			m_stream = new FileStream(m_path, mode, access, share, BUFFER_SIZE, options);
			m_stream.SetLength(m_lengthAligned);

			long startPositionAligned = ((startPosition + (m_sectorSize - 1)) & (~(long)(m_sectorSize - 1))) - m_sectorSize;
			if (startPositionAligned >= 0)
				m_stream.Seek(startPositionAligned, SeekOrigin.Begin);
			else
				startPositionAligned = 0;
			m_bufferIndex = (int)(startPosition - startPositionAligned);
		}

		public override void Write(byte[] array, int offset, int count)
		{
			if (m_actualLength > 0)
				throw new InvalidOperationException("Partial flush already encountered.");

			while (count > 0)
			{
				// copy from array into remaining buffer
				int remainingSpaceInBuffer = m_buffer.Length - m_bufferIndex;
				int bytesToCopy = Math.Min(remainingSpaceInBuffer, count);

				Buffer.BlockCopy(array, offset, m_buffer.Data, m_bufferIndex, bytesToCopy);
				m_bufferIndex += bytesToCopy;
				offset += bytesToCopy;
				count -= bytesToCopy;

				if (m_bufferIndex == m_buffer.Length)
					FlushInternal();
			}
		}

		private void FlushInternal()
		{
			if (m_bufferIndex > 0)
			{
				var sw = Stopwatch.StartNew();

				// a partial flush is only allowed at the end of the file
				if (m_bufferIndex != m_buffer.Length && m_stream.Position + m_bufferIndex != m_length)
				{
					// once this is done, no more writes can be accepted
					m_actualLength = Position;
				}

				m_stream.Write(m_buffer.Data, 0, m_buffer.Length);
				m_bufferIndex = 0;

				sw.Stop();
				PerformanceManager.UpdateWriteBytes(m_actualLength > 0 ? m_actualLength : m_buffer.Length, sw);
			}
		}

		protected override void Dispose(bool disposing)
		{
			FlushInternal();

			long position = Position;

			if (m_stream != null)
			{
				m_stream.Dispose();
				m_stream = null;
			}

			if (m_buffer != null)
			{
				m_buffer.Dispose();
				m_buffer = null;
			}

			// set the correct length
			// this really isn't necessary for intermediate files (segments)
			if (m_truncateOnClose)
			{
				long length = m_actualLength > 0 ? m_actualLength : position;
				using (var stream = new FileStream(m_path, FileMode.Open, FileAccess.Write, FileShare.None, 8, FileOptions.WriteThrough))
				{
					stream.SetLength(length);
				}
			}
		}

		#region Stream Members

		public override long Position
		{
			get { return m_stream.Position + m_bufferIndex; }
			set { throw new InvalidOperationException("The stream does not support seeking"); }
		}

		public override bool CanRead
		{
			get { return false; }
		}

		public override bool CanSeek
		{
			get { return false; }
		}

		public override bool CanWrite
		{
			get { return true; }
		}

		public override void Flush()
		{
			throw new InvalidOperationException("The stream cannot be flushed manually");
		}

		public override long Length
		{
			get { return m_stream.Length; }
		}

		public override int Read(byte[] buffer, int offset, int count)
		{
			throw new InvalidOperationException("The stream is write-only");
		}

		public override long Seek(long offset, SeekOrigin origin)
		{
			throw new InvalidOperationException("The stream does not support seeking");
		}

		public override void SetLength(long value)
		{
			throw new InvalidOperationException("The stream length must be set in the constructor");
		}

		#endregion
	}
}
