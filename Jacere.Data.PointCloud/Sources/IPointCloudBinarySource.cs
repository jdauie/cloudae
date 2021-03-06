﻿using System;
using Jacere.Core.Geometry;

namespace Jacere.Data.PointCloud
{
	public interface IPointCloudBinarySource : IPointCloudBinarySourceSequentialEnumerable
	{
		Extent3D Extent { get; }
		SQuantizedExtent3D QuantizedExtent { get; }
		SQuantization3D Quantization { get; }

		IPointCloudBinarySource CreateSegment(long pointIndex, long pointCount);
		IPointCloudBinarySource CreateSparseSegment(PointCloudBinarySourceEnumeratorSparseRegion regions);
	}
}
