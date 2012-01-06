using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CloudAE.Core
{
	public static class BufferManager
	{
		public static class Sizes
		{
			public const int  MB_1   = 1 << 20;
			public const int  MB_2   = 1 << 21;
			public const int  MB_4   = 1 << 22;
			public const int  MB_8   = 1 << 23;
			public const int  MB_16  = 1 << 24;
			public const int  MB_32  = 1 << 25;
			public const int  MB_64  = 1 << 26;
			public const int  MB_128 = 1 << 27;
			public const int  MB_256 = 1 << 28;
			public const int  MB_512 = 1 << 29;
			public const int  GB_1   = 1 << 30;
			public const long GB_2   = 1 << 31;
		}

		/// <summary>1MB</summary>
		public const int BUFFER_SIZE_BYTES = Sizes.MB_1;

		public const int POINT_SIZE_BYTES = 3 * sizeof(double);

		public const int POINTS_PER_BUFFER = BUFFER_SIZE_BYTES / POINT_SIZE_BYTES;

		public const int USABLE_BYTES_PER_BUFFER = POINTS_PER_BUFFER * POINT_SIZE_BYTES;


		public const int QUANTIZED_POINT_SIZE_BYTES = 3 * sizeof(int);

		public const int QUANTIZED_POINTS_PER_BUFFER = BUFFER_SIZE_BYTES / QUANTIZED_POINT_SIZE_BYTES;

		public const int USABLE_QUANTIZED_BYTES_PER_BUFFER = QUANTIZED_POINTS_PER_BUFFER * QUANTIZED_POINT_SIZE_BYTES;




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
			c_availableBuffers.AddLast(buffer);
		}
	}
}
