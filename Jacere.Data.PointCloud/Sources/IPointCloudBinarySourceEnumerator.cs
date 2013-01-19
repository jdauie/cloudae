using System;
using System.Collections.Generic;
using System.Linq;

namespace Jacere.Data.PointCloud
{
	public interface IPointCloudBinarySourceEnumerator : IPointCloudChunkEnumerator<IPointDataProgressChunk>
	{
	}

	public interface IPointCloudChunkEnumerator<out T> : IEnumerator<T>, IEnumerable<T> where T : IPointDataProgressChunk
	{
	}
}
