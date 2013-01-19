using System;
using System.Runtime.InteropServices;

namespace Jacere.Core
{
	public unsafe class BufferInstance : IDisposable
	{
		private readonly byte[] m_data;
		private readonly int m_length;
		private byte* m_dataPtr;
		private byte* m_dataEndPtr;
		private bool m_pinned;

		private GCHandle m_gcHandle;

		#region Properties

		public byte[] Data
		{
			get { return m_data; }
		}

		public byte* DataPtr
		{
			get { return m_dataPtr; }
		}

		public byte* DataEndPtr
		{
			get { return m_dataEndPtr; }
		}

		public bool Pinned
		{
			get { return m_pinned; }
		}

		public int Length
		{
			get { return m_length; }
		}

		#endregion

		public BufferInstance(byte[] buffer)
		{
			m_data = buffer;
			m_length = m_data.Length;
		}

		public void PinBuffer()
		{
			UnpinBuffer();
			m_gcHandle = GCHandle.Alloc(m_data, GCHandleType.Pinned);
			IntPtr pAddr = Marshal.UnsafeAddrOfPinnedArrayElement(m_data, 0);
			m_dataPtr = (byte*)pAddr.ToPointer();
			m_dataEndPtr = m_dataPtr + m_data.Length;
			m_pinned = true;
		}

		public void UnpinBuffer()
		{
			m_pinned = false;
			m_dataPtr = null;
			m_dataEndPtr = null;
			if (m_gcHandle.IsAllocated)
				m_gcHandle.Free();
		}

		#region IDisposable Members

		public void Dispose()
		{
			BufferManager.ReleaseBuffer(this);
		}

		#endregion
	}
}
