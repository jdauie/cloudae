using System;
using Jacere.Core.Geometry;

namespace CloudAE.Core
{
	public interface IPointCloudBinarySource : IPointCloudBinarySourceSequentialEnumerable
	{
		Extent3D Extent { get; }
		Quantization3D Quantization { get; }

		IPointCloudBinarySource CreateSegment(long pointIndex, long pointCount);
		IPointCloudBinarySource CreateSparseSegment(PointCloudBinarySourceEnumeratorSparseRegion regions);
	}
}
