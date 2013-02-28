using System;

namespace Jacere.Data.PointCloud
{
	public class PointCloudBinarySourceEnumeratorRegion
	{
		public readonly int ChunkStart;
		public readonly int ChunkCount;

		public PointCloudBinarySourceEnumeratorRegion(int chunkStart, int chunkCount)
		{
			ChunkStart = chunkStart;
			ChunkCount = chunkCount;
		}

		public override string ToString()
		{
			return string.Format("[{0}-{1}] ({2})", ChunkStart, ChunkStart + ChunkCount, ChunkCount);
		}
	}
}
