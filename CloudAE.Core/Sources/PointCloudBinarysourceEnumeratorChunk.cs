using System;
using System.Collections.Generic;
using System.Linq;

namespace CloudAE.Core
{
	public unsafe class PointCloudBinarySourceEnumeratorChunk : IProgress
	{
		public readonly uint Index;
		public readonly int BytesRead;
		public readonly byte* DataPtr;
		public readonly byte* DataEndPtr;

		private readonly float m_progress;

		public float Progress
		{
			get { return m_progress; }
		}

		public PointCloudBinarySourceEnumeratorChunk(uint index, BufferInstance buffer, int bytesRead, float progress)
		{
			Index = index;
			BytesRead = bytesRead;
			DataPtr = buffer.DataPtr;
			DataEndPtr = DataPtr + BytesRead;

			m_progress = progress;
		}
	}
}
