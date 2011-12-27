using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using CloudAE.Core.Geometry;
using CloudAE.Core.Compression;
using System.IO;

namespace CloudAE.Core
{
	public class PointCloudBinarySourceSegment : PointCloudBinarySource
	{
		public PointCloudBinarySourceSegment(PointCloudBinarySource source, long segmentPointIndex, long segmentPointCount)
			: base(source.FilePath, segmentPointCount, source.Extent, source.Quantization, source.PointDataOffset + segmentPointIndex * source.PointSizeBytes, source.PointSizeBytes, source.Compression)
		{
		}
	}
}
