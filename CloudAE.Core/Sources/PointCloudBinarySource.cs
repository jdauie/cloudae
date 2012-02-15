using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using CloudAE.Core.Geometry;
using CloudAE.Core.Compression;
using System.IO;

namespace CloudAE.Core
{
	public class PointCloudBinarySource : PointCloudSource
	{
		public const string FILE_EXTENSION = "bin";

		public readonly long Count;
		public readonly Quantization3D Quantization;
		public readonly CompressionMethod Compression;
		public readonly ICompressor Compressor;
		public readonly short PointSizeBytes;
		public readonly int UsableBytesPerBuffer;

		private long m_pointDataOffset;
		private Extent3D m_extent;

		public long PointDataOffset
		{
			get { return m_pointDataOffset; }
			protected set { m_pointDataOffset = value; }
		}

		public Extent3D Extent
		{
			get { return m_extent; }
			set { m_extent = value; }
		}

		public PointCloudBinarySource(string file, long count, Extent3D extent, Quantization3D quantization, long dataOffset, short pointSizeBytes, CompressionMethod compression)
			: base(file)
		{
			Count = count;
			Extent = extent;
			Quantization = quantization;
			Compression = compression;
			Compressor = CompressionFactory.GetCompressor(Compression);
			PointDataOffset = dataOffset;
			PointSizeBytes = pointSizeBytes;
			int pointsPerInputBuffer = BufferManager.BUFFER_SIZE_BYTES / pointSizeBytes;
			UsableBytesPerBuffer = pointsPerInputBuffer * pointSizeBytes;
		}

		public PointCloudBinarySourceEnumerator GetBlockEnumerator(byte[] buffer)
		{
			return new PointCloudBinarySourceEnumerator(this, buffer);
		}
	}
}
