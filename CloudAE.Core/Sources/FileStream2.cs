using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using CloudAE.Core.Windows;
using CloudAE.Core.Util;

namespace CloudAE.Core
{
	/// <summary>
	/// A FileStream wrapper that supports only exclusive read and exclusive write.
	/// 
	/// THIS SHOULD PROBABLY BE SPLIT INTO SEPARATE CLASSES FOR READ/WRITE/SEQUENTIAL/RANDOM.
	/// 
	/// For instance, force WRITE to SetLength, and deny seeking.
	/// </summary>
	public class FileStream2 : IDisposable
	{
		private const FileOptions FileFlagNoBuffering = (FileOptions)0x20000000;

		private const int BUFFER_SIZE = (int)ByteSizesSmall.MB_1;

		private readonly BufferInstance m_buffer;
		private readonly bool m_useCache;

		private readonly uint m_sectorSize;

		private readonly FileStream m_stream;

		private long m_offset;
		private long m_bufferOffset;

		private int m_bufferIndex;

		private long m_position;
		private long m_positionSectorAligned;

		// allow bybassing cache
		// make buffering simple
		// keep pinned buffer if available
		// figure out how to simplify buffer edges since they have to be sector aligned

		// use cases:
		//   Reading input sequentially.
		//   Reading and writing sequentially in parallel?
		//   Writing sequentially.
		//   Reading randomly.

		// I am remembering that with FileCaching, the read,write in parallel (for segments)
		// has already been put in cache (from the previous pass), so it is really just a write.
		// I need to make sure this stays just as fast...not sure how.

		// NO! What I wrote above is wrong for large files...I was conflating different ideas.
		// I do want to figure out a way to keep small files fast.
		// I also need to figure out how to do the merge optimally with smooth progress,
		// but that should probably go outside this scope.

		// Also keep in mind that if I want to read from a network drive, some of this won't work.

		// Should I support random through this mechanism?
		// Should I support random write at all? (probably not for now)

		public FileStream2(string path, bool write, bool cache, bool random)
		{
			m_useCache = cache;

			FileMode   mode   = write ? FileMode.OpenOrCreate : FileMode.Open;
			FileAccess access = write ? FileAccess.Write : FileAccess.Read;
			FileShare  share  = write ? FileShare.None : FileShare.Read;

			FileOptions options = m_useCache ? FileOptions.None : (FileFlagNoBuffering | FileOptions.WriteThrough);
			options |= random ? FileOptions.RandomAccess : FileOptions.SequentialScan;

			if (!m_useCache)
			{
				//m_buffer = BufferManager.AcquireBuffer(null, true);
			}

			m_stream = new FileStream(path, mode, access, share, BUFFER_SIZE, options);

			m_sectorSize = PathUtil.GetDriveSectorSize(path);
		}

		public int Read(byte[] array, int offset, int count)
		{
			int r = 0;

			if (m_useCache)
			{
				r = m_stream.Read(array, offset, count);
			}
			else
			{
				// ensure we are on a sector boundary
				// this is easy for sequential
			}

			return r;
		}

		public void Write(byte[] array, int offset, int count)
		{
			if (m_useCache)
			{
				m_stream.Write(array, offset, count);
			}
			else
			{
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
		}

		private void FlushInternal()
		{
		}

		public void Dispose()
		{
			if (m_stream != null)
				m_stream.Dispose();

			if (m_buffer != null)
				m_buffer.Dispose();
		}
	}
}
