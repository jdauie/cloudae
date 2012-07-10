using System;

namespace CloudAE.Core
{
	public interface IPointCloudBinarySourceEnumerable
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
	}
}
