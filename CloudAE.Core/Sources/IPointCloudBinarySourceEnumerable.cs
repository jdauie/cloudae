using System;
using System.Collections.Generic;

namespace CloudAE.Core
{
	public interface IPointCloudBinarySourceEnumerable
	{
		string FilePath       { get; }
		long   Count          { get; }
		short  PointSizeBytes { get; }

		IEnumerable<string> SourcePaths { get; }

		IPointCloudBinarySourceEnumerator GetBlockEnumerator(BufferInstance buffer);
		IPointCloudBinarySourceEnumerator GetBlockEnumerator(ProgressManagerProcess process);
	}

	public interface IPointCloudBinarySourceSequentialEnumerable : IPointCloudBinarySourceEnumerable
	{
		long PointDataOffset { get; }
	}
}
