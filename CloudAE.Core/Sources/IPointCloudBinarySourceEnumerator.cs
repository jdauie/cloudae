using System;
using System.Collections.Generic;
using System.Linq;

namespace CloudAE.Core
{
	public interface IPointCloudBinarySourceEnumerator : IEnumerator<PointCloudBinarySourceEnumeratorChunk>, IEnumerable<PointCloudBinarySourceEnumeratorChunk>
	{
	}
}
