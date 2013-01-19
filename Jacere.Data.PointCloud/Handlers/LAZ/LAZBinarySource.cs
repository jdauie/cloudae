using System;
using System.Collections.Generic;
using System.Linq;
using Jacere.Data.PointCloud.Geometry;

namespace Jacere.Data.PointCloud
{
	public class LAZBinarySource : PointCloudBinarySource
	{
		private readonly LAZFile m_handler;

		public LAZBinarySource(FileHandlerBase file, long count, Extent3D extent, Quantization3D quantization, long dataOffset, short pointSizeBytes)
			: base(file, count, extent, quantization, dataOffset, pointSizeBytes)
		{
			m_handler = (LAZFile)file;
		}

		public override IStreamReader GetStreamReader()
		{
			return new LAZStreamReader(FilePath, m_handler.Header, m_handler.EncodedVLR);
		}

		public override IPointCloudBinarySource CreateSegment(long pointIndex, long pointCount)
		{
			long offset = PointDataOffset + pointIndex * PointSizeBytes;
			var segment = new LAZBinarySource(m_handler, pointCount, Extent, Quantization, offset, PointSizeBytes);
			return segment;
		}
	}
}
