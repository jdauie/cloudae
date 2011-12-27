using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CloudAE.Core
{
	public class PointCloudBinarySourceEnumeratorChunk
	{
		public readonly float EnumeratorProgress;
		public readonly int BytesRead;

		public PointCloudBinarySourceEnumeratorChunk(int bytesRead, float progress)
		{
			BytesRead = bytesRead;
			EnumeratorProgress = progress;
		}
	}
}
