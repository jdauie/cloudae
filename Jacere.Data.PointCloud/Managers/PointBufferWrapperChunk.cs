using System;
using System.Linq;

namespace Jacere.Core
{
	public unsafe class PointBufferWrapperChunk : IProgress, IPointDataChunk
	{
		private readonly int m_index;

		private readonly int m_bytesRead;
		private readonly int m_pointsRead;
		private readonly byte* m_dataPtr;
		private readonly byte* m_dataEndPtr;

		private readonly BufferInstance m_buffer;
		private readonly short m_pointSizeBytes;
		private readonly float m_progress;

		public float Progress
		{
			get { return m_progress; }
		}

		#region IPointDataChunk Members

		public int Index
		{
			get { return m_index; }
		}

		byte[] IPointDataChunk.Data
		{
			get { return m_buffer.Data; }
		}

		public byte* PointDataPtr
		{
			get { return m_dataPtr; }
		}

		public byte* PointDataEndPtr
		{
			get { return m_dataEndPtr; }
		}

		public int Length
		{
			get { return (int)(m_dataEndPtr - m_dataPtr); }
		}

		public short PointSizeBytes
		{
			get { return m_pointSizeBytes; }
		}

		public int PointCount
		{
			get { return m_pointsRead; }
		}

		public IPointDataChunk CreateSegment(int pointCount)
		{
			return new PointBufferWrapperChunk(m_index, m_buffer, 0, 0, m_pointSizeBytes, m_progress);
		}

		#endregion

		public PointBufferWrapperChunk(int index, BufferInstance buffer, int byteIndex, int byteLength, short pointSizeBytes, float progress)
		{
			m_buffer = buffer;
			m_index = index;
			m_pointSizeBytes = pointSizeBytes;
			m_bytesRead = byteLength;
			m_pointsRead = m_bytesRead / m_pointSizeBytes;
			m_dataPtr = buffer.DataPtr + byteIndex;
			m_dataEndPtr = m_dataPtr + m_bytesRead;

			m_progress = progress;
		}

		public PointBufferWrapperChunk(PointBufferWrapperChunk chunk, int pointCount)
		{
			m_buffer = chunk.m_buffer;
			m_index = chunk.m_index;
			m_pointSizeBytes = chunk.m_pointSizeBytes;
			m_pointsRead = pointCount;
			m_bytesRead = m_pointsRead * m_pointSizeBytes;
			m_dataPtr = chunk.m_dataPtr;
			m_dataEndPtr = m_dataPtr + m_bytesRead;

			m_progress = chunk.m_progress;
		}
	}
}
