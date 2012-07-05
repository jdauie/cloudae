using System;
using System.Collections.Generic;
using System.Linq;

namespace CloudAE.Core
{
	public interface IPointDataChunk
	{
		byte[] Data { get; }
		unsafe byte* PointDataPtr { get; }
		unsafe byte* PointDataEndPtr { get; }
		int Length { get; }
		short PointSizeBytes { get; }
		int PointCount { get; }
	}

	public unsafe class PointCloudBinarySourceEnumeratorChunk : IProgress, IPointDataChunk
	{
		public readonly uint Index;
		public readonly int BytesRead;
		public readonly int PointsRead;
		public readonly byte* DataPtr;
		public readonly byte* DataEndPtr;

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
			get { return DataPtr; }
		}

		public byte* PointDataEndPtr
		{
			get { return DataEndPtr; }
		}

		public int Length
		{
			get { return (int)(DataEndPtr - DataPtr); }
		}

		public short PointSizeBytes
		{
			get { return m_pointSizeBytes; }
		}

		public int PointCount
		{
			get { return PointsRead; }
		}

		#endregion

		public PointCloudBinarySourceEnumeratorChunk(uint index, BufferInstance buffer, int bytesRead, short pointSizeBytes, float progress)
		{
			m_buffer = buffer;
			Index = index;
			m_pointSizeBytes = pointSizeBytes;
			BytesRead = bytesRead;
			PointsRead = BytesRead / m_pointSizeBytes;
			DataPtr = buffer.DataPtr;
			DataEndPtr = DataPtr + BytesRead;

			m_progress = progress;
		}
	}
}
