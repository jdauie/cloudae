using System;
using System.Collections.Generic;
using System.Linq;

namespace CloudAE.Core
{
	public class PointCloudBinarySourceSegment : PointCloudBinarySource
	{
		public PointCloudBinarySourceSegment(IPointCloudBinarySource source, long segmentPointIndex, long segmentPointCount)
			: base(source.FilePath, segmentPointCount, source.Extent, source.Quantization, source.PointDataOffset + segmentPointIndex * source.PointSizeBytes, source.PointSizeBytes)
		{
		}
	}
}
