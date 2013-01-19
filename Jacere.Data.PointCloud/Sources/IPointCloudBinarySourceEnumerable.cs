using System;
using System.Collections.Generic;
using Jacere.Core;

namespace Jacere.Data.PointCloud
{
	public interface IPointCloudBinarySourceEnumerable : ISourcePaths
	{
		string FilePath       { get; }
		long   Count          { get; }
		short  PointSizeBytes { get; }

		IPointCloudBinarySourceEnumerator GetBlockEnumerator(BufferInstance buffer);
		IPointCloudBinarySourceEnumerator GetBlockEnumerator(ProgressManagerProcess process);
	}

	public interface IPointCloudBinarySourceSequentialEnumerable : IPointCloudBinarySourceEnumerable
	{
		long PointDataOffset { get; }

		IStreamReader GetStreamReader();
	}
}
