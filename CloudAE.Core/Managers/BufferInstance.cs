using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;

namespace CloudAE.Core
{
	public unsafe class BufferInstance
	{
		public byte[] m_data;
		public byte* m_dataPtr;
		public byte* m_dataEndPtr;
		public bool m_pinned;
		public int m_length;

		private GCHandle m_gcHandle;

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
	}
}
