using System;

using CloudAE.Core.Geometry;

namespace CloudAE.Core
{
	public interface IPointCloudBinarySourceEnumerable
	{
		string FilePath { get; }
		long Count { get; }
		long PointDataOffset { get; }
		short PointSizeBytes { get; }
		int UsableBytesPerBuffer { get; }
		int PointsPerBuffer { get; }
		PointCloudBinarySourceEnumerator GetBlockEnumerator(byte[] buffer);
	}
}
