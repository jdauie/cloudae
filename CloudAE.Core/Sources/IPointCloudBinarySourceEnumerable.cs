using System;

namespace CloudAE.Core
{
	public interface IPointCloudBinarySourceEnumerable
	{
		string FilePath             { get; }
		long   Count                { get; }
		long   PointDataOffset      { get; }
		short  PointSizeBytes       { get; }
		int    UsableBytesPerBuffer { get; }
		int    PointsPerBuffer      { get; }

		IPointCloudBinarySourceEnumerator GetBlockEnumerator(BufferInstance buffer);
		IPointCloudBinarySourceEnumerator GetBlockEnumerator(ProgressManagerProcess process);
	}
}
