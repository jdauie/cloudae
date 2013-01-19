using System;

namespace Jacere.Data.PointCloud
{
	public interface IPointDataChunk
	{
		int Index { get; }
		byte[] Data { get; }
		unsafe byte* PointDataPtr { get; }
		unsafe byte* PointDataEndPtr { get; }
		int Length { get; }
		short PointSizeBytes { get; }
		int PointCount { get; }

		IPointDataChunk CreateSegment(int pointCount);
	}

	public interface IPointDataProgressChunk : IPointDataChunk, IProgress
	{
	}
}
