using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace CloudAE.Core
{
	public enum ByteSizesSmall : int
	{
		KB_128 = 1 << 17,
		KB_256 = 1 << 18,
		KB_512 = 1 << 19,
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

		// eventually, this should handle buffers of varying size,
		// or at least deallocate abnormal-sized buffers
		private static readonly Dictionary<int, Stack<BufferInstance>> c_availableBuffersBySize;
		private static readonly Dictionary<byte[], BufferInstance> c_bufferMapping;

		private static readonly Dictionary<BufferInstance, Identity> c_usedBuffers;

		private static readonly Identity c_id;

		static BufferManager()
		{
			c_id = IdentityManager.AcquireIdentity(typeof(BufferManager).Name);

			c_availableBuffersBySize = new Dictionary<int, Stack<BufferInstance>>();
			c_availableBuffersBySize.Add(BUFFER_SIZE_BYTES, new Stack<BufferInstance>());
			c_bufferMapping = new Dictionary<byte[], BufferInstance>();
			c_usedBuffers = new Dictionary<BufferInstance, Identity>();
		}

		private static Stack<BufferInstance> GetAvailableBuffers(int size, bool createIfNecessary)
		{
			if (c_availableBuffersBySize.ContainsKey(size))
			{
				return c_availableBuffersBySize[size];
			}
			else if (createIfNecessary)
			{
				Stack<BufferInstance> newBufferList = new Stack<BufferInstance>();
				c_availableBuffersBySize.Add(size, newBufferList);
				return newBufferList;
			}

			return null;
		}

		public static BufferInstance AcquireBuffer()
		{
			// Since we don't know what this is used for, the manager itself will take ownership.
			return AcquireBuffer(c_id, BUFFER_SIZE_BYTES, false);
		}

		public static BufferInstance AcquireBuffer(Identity id)
		{
			return AcquireBuffer(id, BUFFER_SIZE_BYTES, false);
		}

		public static BufferInstance AcquireBuffer(Identity id, bool pin)
		{
			return AcquireBuffer(id, BUFFER_SIZE_BYTES, pin);
		}

		public static BufferInstance AcquireBuffer(Identity id, int size)
		{
			return AcquireBuffer(id, size, false);
		}

		public static BufferInstance AcquireBuffer(Identity id, int size, bool pin)
		{
			// make sure size is reasonable

			BufferInstance buffer = null;

			lock (typeof(BufferManager))
			{
				Stack<BufferInstance> availableBuffers = GetAvailableBuffers(size, false);
				if (availableBuffers != null && availableBuffers.Count > 0)
				{
					buffer = availableBuffers.Pop();
				}
				else
				{
					byte[] b = new byte[size];
					buffer = new BufferInstance(b);
					c_bufferMapping.Add(b, buffer);
				}
				c_usedBuffers.Add(buffer, id);

				if (pin)
					buffer.PinBuffer();
			}

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
					throw new Exception("attempted to release a buffer that is not in use");

				c_usedBuffers.Remove(buffer);

				Stack<BufferInstance> bufferList = GetAvailableBuffers(buffer.Length, true);
				bufferList.Push(buffer);

				if (buffer.Pinned)
					buffer.UnpinBuffer();
			}
		}

		public static void ReleaseBuffers(Identity id)
		{
			lock (typeof(BufferManager))
			{
				ReleaseBuffers(c_usedBuffers.Where(kvp => kvp.Value == id).Select(kvp => kvp.Key).ToArray());
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
#warning I don't run in debug much -- I need a proper log file for this kind of thing
				Debug.Assert(c_usedBuffers.Count == 0, String.Format("{0} buffers were not released", c_usedBuffers.Count));

				ReleaseBuffers(c_usedBuffers.Keys.ToArray());
			}
		}
	}
}
