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
		private static readonly LinkedList<byte[]> c_availableBuffers;

		// the identifier should possibly be more than a string in the future
		private static readonly Dictionary<byte[], string> c_usedBuffers;

		static BufferManager()
		{
			c_availableBuffers = new LinkedList<byte[]>();
			c_usedBuffers = new Dictionary<byte[], string>();
		}

		public static byte[] AcquireBuffer()
		{
			return AcquireBuffer(string.Empty);
		}

		public static byte[] AcquireBuffer(string name)
		{
			byte[] buffer = null;
			lock (typeof(BufferManager))
			{
				if (c_availableBuffers.Count > 0)
				{
					buffer = c_availableBuffers.First.Value;
					c_availableBuffers.RemoveFirst();
				}
				else
				{
					buffer = new byte[BUFFER_SIZE_BYTES];
				}
			}
			c_usedBuffers.Add(buffer, name);
			return buffer;
		}

		public static void ReleaseBuffer(byte[] buffer)
		{
			lock (typeof(BufferManager))
			{
				if (!c_usedBuffers.ContainsKey(buffer))
					throw new Exception("attempted to release a buffer that does not belong");

				c_usedBuffers.Remove(buffer);
				c_availableBuffers.AddLast(buffer);
			}
		}

		public static void ReleaseBuffers(string name)
		{
			lock (typeof(BufferManager))
			{
				byte[][] buffersToRelease = c_usedBuffers.Where(kvp => kvp.Value == name).Select(kvp => kvp.Key).ToArray();
				foreach (byte[] buffer in buffersToRelease)
					ReleaseBuffer(buffer);
			}
		}

		internal static void Shutdown()
		{
			Debug.Assert(c_usedBuffers.Count == 0, String.Format("{0} buffers were not released", c_usedBuffers.Count));
		}
	}
}
