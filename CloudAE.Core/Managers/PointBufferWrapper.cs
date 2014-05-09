using System;
using System.Collections.Generic;
using System.Linq;

using Jacere.Core;
using Jacere.Data.PointCloud;

namespace CloudAE.Core
{
	public unsafe class PointBufferWrapper : IPointDataChunk, IEnumerable<IPointDataChunk>, IChunkProcess
	{
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

		public int Index
		{
			get { return 0; }
		}

		public byte* PointDataPtr
		{
			get { return m_pointDataPtr; }
		}

		public byte* PointDataEndPtr
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

		public IPointDataChunk CreateSegment(int pointCount)
		{
			if (pointCount > m_pointCount)
				throw new Exception("Too many points");

			return new PointBufferWrapper(m_buffer, m_pointSizeBytes, pointCount);
		}

		#endregion

		/// <summary>
		/// Initializes a new instance of the <see cref="PointBufferWrapper"/> class,
		/// for wrapping a binary source segment buffer.
		/// </summary>
		/// <param name="buffer">The buffer.</param>
		/// <param name="source">The source.</param>
		/// <param name="pointCount">The point count.</param>
		public PointBufferWrapper(BufferInstance buffer, IPointCloudBinarySource source, int pointCount)
		{
			m_buffer = buffer;

			m_pointCount = pointCount;
			m_pointSizeBytes = source.PointSizeBytes;
			m_length = m_pointCount * m_pointSizeBytes;
			m_pointDataPtr = m_buffer.DataPtr;
			m_pointDataEndPtr = m_pointDataPtr + m_length;

			m_bufferIndex = 0;
		}

		public PointBufferWrapper(BufferInstance buffer, short pointSizeBytes, int pointCount)
		{
			m_buffer = buffer;

			m_pointCount = pointCount;
			m_pointSizeBytes = pointSizeBytes;
			m_length = m_pointCount * m_pointSizeBytes;
			m_pointDataPtr = m_buffer.DataPtr;
			m_pointDataEndPtr = m_pointDataPtr + m_length;

			m_bufferIndex = 0;
		}

		public PointBufferWrapper(BufferInstance buffer, IPointCloudBinarySource source)
			: this(buffer, source, (int)source.Count)
		{
		}

		private PointBufferWrapper(PointBufferWrapper wrapper, bool initialized)
			: this(wrapper.m_buffer, wrapper.m_pointSizeBytes, wrapper.m_pointCount)
		{
			m_initialized = initialized;
		}

		public void Append(IPointDataChunk chunk)
		{
			if (m_initialized)
				throw new InvalidOperationException("Cannot append to initialized buffer");

			if (m_bufferIndex + chunk.Length > m_buffer.Data.Length)
				throw new Exception("Too much data");

			Buffer.BlockCopy(chunk.Data, 0, m_buffer.Data, m_bufferIndex, chunk.Length);
			m_bufferIndex += chunk.Length;
		}

		public IPointDataChunk Process(IPointDataChunk chunk)
		{
			Append(chunk);
			return chunk;
		}

		public PointBufferWrapper Initialize()
		{
			return new PointBufferWrapper(this, true);
		}

		#region IEnumerable Members

		public IEnumerator<IPointDataChunk> GetEnumerator()
		{
			const int intervalSize = BufferManager.BUFFER_SIZE_BYTES;
			float intervals = (float)Math.Ceiling((float)m_length / intervalSize);

			int index = 0;
			int remainingBytes = m_length;

			while (remainingBytes > 0)
			{
				int currentBytes = Math.Min(intervalSize, remainingBytes);
				yield return new PointBufferWrapperChunk(index, m_buffer, m_length - remainingBytes, currentBytes, m_pointSizeBytes, index / intervals);

				remainingBytes -= currentBytes;
				++index;
			}
		}

		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		#endregion
	}
}
