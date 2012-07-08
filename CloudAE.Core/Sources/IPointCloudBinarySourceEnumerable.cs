using System;

namespace CloudAE.Core
{
	public interface IPointCloudBinarySourceEnumerable
	{
		string FilePath             { get; }
		long   Count                { get; }
		long   PointDataOffset      { get; }
		short  PointSizeBytes       { get; }

		IPointCloudBinarySourceEnumerator GetBlockEnumerator(BufferInstance buffer);
		IPointCloudBinarySourceEnumerator GetBlockEnumerator(ProgressManagerProcess process);
	}
}
