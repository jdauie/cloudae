using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CloudAE.Core
{
	public class PointCloudBinarySourceEnumeratorChunk : IProgress
	{
		public readonly int BytesRead;

		private readonly float m_progress;

		public float Progress
		{
			get { return m_progress; }
		}

		public PointCloudBinarySourceEnumeratorChunk(int bytesRead, float progress)
		{
			BytesRead = bytesRead;
			m_progress = progress;
		}
	}
}
