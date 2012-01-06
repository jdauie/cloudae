using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CloudAE.Core
{
	class BufferManager
	{
		/// <summary>1MB</summary>
		public const int BUFFER_SIZE_BYTES = 2 << 19;

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
