using System;
using System.Collections.Generic;
using System.Linq;

namespace CloudAE.Core
{
	public interface IPointDataChunk
	{
		unsafe byte* PointDataPtr { get; }
		unsafe byte* PointDataEndPtr { get; }
		short PointSizeBytes { get; }
		int PointCount { get; }
	}

	public unsafe class PointCloudBinarySourceEnumeratorChunk : IProgress, IPointDataChunk
	{
		public readonly uint Index;
		public readonly int BytesRead;
		public readonly int PointsRead;
		public readonly byte[] Data;
		public readonly byte* DataPtr;
		public readonly byte* DataEndPtr;
		public readonly int Length;

		private readonly short m_pointSizeBytes;
		private readonly float m_progress;

		public float Progress
		{
			get { return m_progress; }
		}

		#region IPointDataChunk Members

		public byte* PointDataPtr
		{
			get { return DataPtr; }
		}

		public byte* PointDataEndPtr
		{
			get { return DataEndPtr; }
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
			Index = index;
			m_pointSizeBytes = pointSizeBytes;
			BytesRead = bytesRead;
			PointsRead = BytesRead / m_pointSizeBytes;
			Data = buffer.Data;
			DataPtr = buffer.DataPtr;
			DataEndPtr = DataPtr + BytesRead;
			Length = (int)(DataEndPtr - DataPtr);

			m_progress = progress;
		}
	}
}
