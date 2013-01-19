using System;
using Jacere.Data.PointCloud.Geometry;

namespace Jacere.Data.PointCloud
{
	public interface IPointCloudBinarySource : IPointCloudBinarySourceSequentialEnumerable
	{
		Extent3D Extent { get; }
		Quantization3D Quantization { get; }

		IPointCloudBinarySource CreateSegment(long pointIndex, long pointCount);
		IPointCloudBinarySource CreateSparseSegment(PointCloudBinarySourceEnumeratorSparseRegion regions);
	}
}
