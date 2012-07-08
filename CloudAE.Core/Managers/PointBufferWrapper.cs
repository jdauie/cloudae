using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CloudAE.Core
{
	public unsafe class PointBufferWrapper : IPointDataChunk
	{
		private readonly IPointCloudBinarySource m_source;
		private readonly BufferInstance m_buffer;
		private readonly byte* m_pointDataPtr;
		private readonly byte* m_pointDataEndPtr;
		private readonly short m_pointSizeBytes;
		private readonly int m_pointCount;
		private readonly int m_length;
		private readonly bool m_initialized;

		private int m_bufferIndex;

		public bool Initialized
		{
			get { return m_initialized; }
		}

		#region BufferInstance Members

		public int Length
		{
			get { return m_length; }
		}

		public byte[] Data
		{
			get { return m_buffer.Data; }
		}

		#endregion

		#region IPointDataChunk Members

		public unsafe byte* PointDataPtr
		{
			get { return m_pointDataPtr; }
		}

		public unsafe byte* PointDataEndPtr
		{
			get { return m_pointDataEndPtr; }
		}

		public short PointSizeBytes
		{
			get { return m_pointSizeBytes; }
		}

		public int PointCount
		{
			get { return m_pointCount; }
		}

		#endregion

		public PointBufferWrapper(BufferInstance buffer, IPointCloudBinarySource source)
		{
			m_buffer = buffer;
			m_source = source;

			m_pointCount = (int)m_source.Count;
			m_pointSizeBytes = m_source.PointSizeBytes;
			m_length = m_pointCount * m_pointSizeBytes;
			m_pointDataPtr = m_buffer.DataPtr;
			m_pointDataEndPtr = m_pointDataPtr + m_length;

			m_bufferIndex = 0;
		}

		private PointBufferWrapper(PointBufferWrapper wrapper, bool initialized)
			: this(wrapper.m_buffer, wrapper.m_source)
		{
			m_initialized = initialized;
		}

		public void Append(IPointDataChunk chunk)
		{
			if (m_initialized)
				throw new InvalidOperationException("Cannot append to initialized buffer");

			Buffer.BlockCopy(chunk.Data, 0, m_buffer.Data, m_bufferIndex, chunk.Length);
			m_bufferIndex += chunk.Length;
		}

		public PointBufferWrapper Initialize()
		{
			return new PointBufferWrapper(this, true);
		}
	}
}
