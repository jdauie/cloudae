using System;
using System.Collections.Generic;
using System.Linq;

namespace Jacere.Core
{
	public class StreamManager
	{
		public class StreamManagerSharedStream : IDisposable
		{
			private readonly ISourcePaths m_source;

			public StreamManagerSharedStream(ISourcePaths source)
			{
				m_source = source;
			}

			public void Dispose()
			{
				StreamManager.DisposeStream(m_source);
			}
		}

		// this might keep too many file handles open when processing hundreds of archive tiles
		private const bool SUPPORT_SHARED_STREAMS = false;

		private static readonly Dictionary<string, FileStreamUnbufferedSequentialRead> c_sharedStreams;

		static StreamManager()
		{
			c_sharedStreams = new Dictionary<string, FileStreamUnbufferedSequentialRead>(StringComparer.OrdinalIgnoreCase);
		}

		public static bool IsSharedStream(IStreamReader reader)
		{
			if (SUPPORT_SHARED_STREAMS)
				return c_sharedStreams.ContainsKey(reader.Path);
			else
				return false;
		}

		public static StreamManagerSharedStream CreateSharedStream(ISourcePaths source)
		{
			foreach (string path in source.SourcePaths)
			{
				if (c_sharedStreams.ContainsKey(path))
					throw new Exception("Stream location already shared");

				c_sharedStreams.Add(path, null);
			}
			
			var shared = new StreamManagerSharedStream(source);

			return shared;
		}

		private static void DisposeStream(ISourcePaths source)
		{
			foreach (string path in source.SourcePaths)
			{
				if (c_sharedStreams.ContainsKey(path))
				{
					var stream = c_sharedStreams[path];
					c_sharedStreams.Remove(path);
					if (stream != null)
						stream.Dispose();
				}
			}
		}

		public static FileStreamUnbufferedSequentialRead OpenReadStream(string path)
		{
			return OpenReadStream(path, 0);
		}

		public static FileStreamUnbufferedSequentialRead OpenReadStream(string path, long start)
		{
			if (SUPPORT_SHARED_STREAMS)
			{
				FileStreamUnbufferedSequentialRead stream = null;
				bool shared = c_sharedStreams.ContainsKey(path);

				if (shared)
					stream = c_sharedStreams[path];

				if (stream == null)
				{
					stream = new FileStreamUnbufferedSequentialRead(path, start);

					if (shared)
						c_sharedStreams[path] = stream;
				}

				return stream;
			}
			else
			{
				return new FileStreamUnbufferedSequentialRead(path, start);
			}
		}

		public static FileStreamUnbufferedSequentialWrite OpenWriteStream(string path, long length, long start)
		{
			return OpenWriteStream(path, length, start, false);
		}

		public static FileStreamUnbufferedSequentialWrite OpenWriteStream(string path, long length, long start, bool truncateOnClose)
		{
			return new FileStreamUnbufferedSequentialWrite(path, length, start, truncateOnClose);
		}
	}
}
