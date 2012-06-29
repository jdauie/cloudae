using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using CloudAE.Core.Windows;

namespace CloudAE.Core.Sources
{
	public class FileStream2 : IDisposable
	{
		private const FileOptions FileFlagNoBuffering = (FileOptions)0x20000000;

		private const int BUFFER_SIZE = (int)ByteSizesSmall.MB_1;

		private readonly BufferInstance m_buffer;
		private readonly bool m_useCache;

		private FileStream m_stream;

		private long m_offset;
		private long m_bufferOffset;

		private int m_bufferIndex;

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

		public FileStream2(string path, FileMode mode, FileAccess access, FileShare share, bool cache, bool random)
		{
			m_useCache = cache;

			FileOptions options = FileOptions.None;

			if (!m_useCache)
			{
				//m_buffer = BufferManager.AcquireBuffer(null, true);
				options |= (FileFlagNoBuffering | FileOptions.WriteThrough);
			}

			if (random)
				options |= FileOptions.RandomAccess;
			else
				options |= FileOptions.SequentialScan;

			m_stream = new FileStream(path, mode, access, share, BUFFER_SIZE, options);
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

			}
		}

		public void Dispose()
		{
			if (m_stream != null)
			{
				m_stream.Dispose();
				m_stream = null;
			}
		}
	}
}
