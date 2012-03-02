using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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

		private static readonly LinkedList<byte[]> c_availableBuffers;

		static BufferManager()
		{
			c_availableBuffers = new LinkedList<byte[]>();
		}

		public static byte[] AcquireBuffer()
		{
			lock (typeof(BufferManager))
			{
				if (c_availableBuffers.Count > 0)
				{
					byte[] buffer = c_availableBuffers.First.Value;
					c_availableBuffers.RemoveFirst();
					return buffer;
				}
				else
				{
					return new byte[BUFFER_SIZE_BYTES];
				}
			}
		}

		public static void ReleaseBuffer(byte[] buffer)
		{
			// I should just add a dictionary of these instead
			if (buffer.Length != BUFFER_SIZE_BYTES)
				throw new Exception("attempted to release a buffer that does not belong");

			c_availableBuffers.AddLast(buffer);
		}
	}
}
