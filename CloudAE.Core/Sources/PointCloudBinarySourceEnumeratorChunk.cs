using System;
using System.Collections.Generic;
using System.Linq;

namespace CloudAE.Core
{
	public unsafe class PointCloudBinarySourceEnumeratorChunk : IProgress, IPointDataChunk
	{
		public readonly uint Index;

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

		#endregion

		public PointCloudBinarySourceEnumeratorChunk(uint index, BufferInstance buffer, int bytesRead, short pointSizeBytes, float progress)
		{
			m_buffer = buffer;
			Index = index;
			m_pointSizeBytes = pointSizeBytes;
			m_bytesRead = bytesRead;
			m_pointsRead = m_bytesRead / m_pointSizeBytes;
			m_dataPtr = buffer.DataPtr;
			m_dataEndPtr = m_dataPtr + m_bytesRead;

			m_progress = progress;
		}
	}
}
