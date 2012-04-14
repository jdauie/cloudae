using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace CloudAE.Core
{
	public enum ByteSizesSmall : int
	{
		MB_1   = 1 << 20,
		MB_2   = 1 << 21,
		MB_4   = 1 << 22,
		MB_8   = 1 << 23,
		MB_16  = 1 << 24,
		MB_32  = 1 << 25,
		MB_64  = 1 << 26,
		MB_128 = 1 << 27,
		MB_256 = 1 << 28,
		MB_512 = 1 << 29,
		GB_1   = 1 << 30
	}

	public enum ByteSizesLarge : long
	{
		MB_1   = (long)1 << 20,
		MB_2   = (long)1 << 21,
		MB_4   = (long)1 << 22,
		MB_8   = (long)1 << 23,
		MB_16  = (long)1 << 24,
		MB_32  = (long)1 << 25,
		MB_64  = (long)1 << 26,
		MB_128 = (long)1 << 27,
		MB_256 = (long)1 << 28,
		MB_512 = (long)1 << 29,
		GB_1   = (long)1 << 30,
		GB_2   = (long)1 << 31,
		GB_4   = (long)1 << 32,
	}

	public static class BufferManager
	{
		public const int BUFFER_SIZE_BYTES = (int)ByteSizesSmall.MB_1;

		public const int QUANTIZED_POINT_SIZE_BYTES = 3 * sizeof(int);

		// eventually, this should handle buffers of varying size,
		// or at least deallocate abnormal-sized buffers
		private static readonly LinkedList<BufferInstance> c_availableBuffers;
		private static readonly Dictionary<byte[], BufferInstance> c_bufferMapping;

		// the identifier should possibly be more than a string in the future
		private static readonly Dictionary<BufferInstance, string> c_usedBuffers;


		static BufferManager()
		{
			c_availableBuffers = new LinkedList<BufferInstance>();
			c_bufferMapping = new Dictionary<byte[], BufferInstance>();
			c_usedBuffers = new Dictionary<BufferInstance, string>();
		}

		public static BufferInstance AcquireBuffer()
		{
			return AcquireBuffer(string.Empty);
		}

		public static BufferInstance AcquireBuffer(string name)
		{
			BufferInstance buffer = null;
			lock (typeof(BufferManager))
			{
				if (c_availableBuffers.Count > 0)
				{
					buffer = c_availableBuffers.First.Value;
					c_availableBuffers.RemoveFirst();
				}
				else
				{
					byte[] b = new byte[BUFFER_SIZE_BYTES];
					buffer = new BufferInstance(b);
					c_bufferMapping.Add(b, buffer);
				}
			}
			c_usedBuffers.Add(buffer, name);
			return buffer;
		}

		public static void ReleaseBuffer(byte[] buffer)
		{
			lock (typeof(BufferManager))
			{
				if (!c_bufferMapping.ContainsKey(buffer))
					throw new Exception("attempted to release a buffer that has no mapping");

				ReleaseBuffer(c_bufferMapping[buffer]);
			}
		}

		public static void ReleaseBuffer(BufferInstance buffer)
		{
			lock (typeof(BufferManager))
			{
				if (!c_usedBuffers.ContainsKey(buffer))
					throw new Exception("attempted to release a buffer that does not belong");

				c_usedBuffers.Remove(buffer);
				c_availableBuffers.AddLast(buffer);

				if (buffer.Pinned)
					buffer.UnpinBuffer();
			}
		}

		public static void ReleaseBuffers(string name)
		{
			lock (typeof(BufferManager))
			{
				ReleaseBuffers(c_usedBuffers.Where(kvp => kvp.Value == name).Select(kvp => kvp.Key).ToArray());
			}
		}

		private static void ReleaseBuffers(BufferInstance[] buffers)
		{
			lock (typeof(BufferManager))
			{
				foreach (BufferInstance buffer in buffers)
					ReleaseBuffer(buffer);
			}
		}

		internal static void Shutdown()
		{
			lock (typeof(BufferManager))
			{
				Debug.Assert(c_usedBuffers.Count == 0, String.Format("{0} buffers were not released", c_usedBuffers.Count));

				ReleaseBuffers(c_usedBuffers.Keys.ToArray());
			}
		}
	}
}
