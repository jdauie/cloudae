using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using CloudAE.Core.Windows;
using CloudAE.Core.Util;

namespace CloudAE.Core
{
	public class FileStreamUnbufferedSequentialRead : IStreamReader
	{
		private const FileOptions FileFlagNoBuffering = (FileOptions)0x20000000;

		private const int BUFFER_SIZE = (int)ByteSizesSmall.MB_1;

		private readonly Identity m_id;
		private readonly uint m_sectorSize;
		private readonly string m_path;

		private BufferInstance m_buffer;
		private FileStream m_stream;
		private int m_bufferIndex;
		private bool m_bufferIsValid;

		public long Position
		{
			get { return m_stream.Position + m_bufferIndex; }
		}

		public FileStreamUnbufferedSequentialRead(string path)
			: this(path, 0)
		{
		}

		public FileStreamUnbufferedSequentialRead(string path, long startPosition)
		{
			m_path = path;
			m_id = IdentityManager.AcquireIdentity(string.Format("{0}:{1}", this.GetType().Name, m_path));
			m_buffer = BufferManager.AcquireBuffer(m_id, true);
			m_sectorSize = PathUtil.GetDriveSectorSize(m_path);

			FileMode    mode    = FileMode.Open;
			FileAccess  access  = FileAccess.Read;
			FileShare   share   = FileShare.Read;// EXCLUSIVE?
			FileOptions options = (FileFlagNoBuffering | FileOptions.WriteThrough | FileOptions.SequentialScan);

			m_stream = new FileStream(m_path, mode, access, share, BUFFER_SIZE, options);

			Seek(startPosition);
		}

		public void Seek(long position)
		{
			if (Position != position && position != 0)
			{
				long positionAligned = ((position + (m_sectorSize - 1)) & (~(long)(m_sectorSize - 1))) - m_sectorSize;
				m_stream.Seek(positionAligned, SeekOrigin.Begin);
				m_bufferIndex = (int)(position - positionAligned);
			}
		}

		public int Read(byte[] array, int offset, int count)
		{
			int bytesToRead = count;
			while (bytesToRead > 0)
			{
				if (!m_bufferIsValid || m_bufferIndex == m_buffer.Length)
					ReadInternal();

				// copy from array into remaining buffer
				int remainingDataInBuffer = m_buffer.Length - m_bufferIndex;
				int bytesToCopy = Math.Min(remainingDataInBuffer, bytesToRead);

				Buffer.BlockCopy(m_buffer.Data, m_bufferIndex, array, offset, bytesToCopy);
				m_bufferIndex += bytesToCopy;
				offset += bytesToCopy;
				bytesToRead -= bytesToCopy;
			}

			return count;
		}

		private void ReadInternal()
		{
			// a partial read is required at the end of the file
			long position = m_stream.Position;
			if (position + m_buffer.Length > m_stream.Length)
			{
				m_stream.Dispose();
				m_stream = null;

				using (var stream = new FileStream(m_path, FileMode.Open, FileAccess.Read, FileShare.None, BUFFER_SIZE, FileOptions.WriteThrough))
				{
					stream.Seek(position, SeekOrigin.Begin);
					stream.Read(m_buffer.Data, 0, (int)(stream.Length - position));
				}
			}
			else
			{
				var a = m_stream.Read(m_buffer.Data, 0, m_buffer.Length);
			}

			// if the buffer was not valid, we just did a seek
			// and need to maintain the buffer index
			if (m_bufferIsValid)
				m_bufferIndex = 0;
			else
				m_bufferIsValid = true;
		}

		public void Dispose()
		{
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
		}
	}
}
