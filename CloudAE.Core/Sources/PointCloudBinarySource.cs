using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using CloudAE.Core.Geometry;
using CloudAE.Core.Compression;
using System.IO;

namespace CloudAE.Core
{
	public class PointCloudBinarySource : PointCloudSource, IPointCloudBinarySourceEnumerable
	{
		public const string FILE_EXTENSION = "bin";

		private readonly long m_count;
		private readonly Quantization3D m_quantization;
		private readonly CompressionMethod m_compression;
		private readonly ICompressor m_compressor;
		private readonly short m_pointSizeBytes;
		private readonly int m_usableBytesPerBuffer;

		private long m_pointDataOffset;
		private Extent3D m_extent;

		#region Properties

		public long Count
		{
			get { return m_count; }
		}

		public Quantization3D Quantization
		{
			get { return m_quantization; }
		}

		public CompressionMethod Compression
		{
			get { return m_compression; }
		}

		public ICompressor Compressor
		{
			get { return m_compressor; }
		}

		public short PointSizeBytes
		{
			get { return m_pointSizeBytes; }
		}

		public int UsableBytesPerBuffer
		{
			get { return m_usableBytesPerBuffer; }
		}

		public long PointDataOffset
		{
			get { return m_pointDataOffset; }
			protected set { m_pointDataOffset = value; }
		}

		public Extent3D Extent
		{
			get { return m_extent; }
			protected set { m_extent = value; }
		}

		#endregion

		public PointCloudBinarySource(string file, long count, Extent3D extent, Quantization3D quantization, long dataOffset, short pointSizeBytes, CompressionMethod compression)
			: base(file)
		{
			m_count = count;
			Extent = extent;
			m_quantization = quantization;
			m_compression = compression;
			m_compressor = CompressionFactory.GetCompressor(Compression);
			PointDataOffset = dataOffset;
			m_pointSizeBytes = pointSizeBytes;
			int pointsPerInputBuffer = BufferManager.BUFFER_SIZE_BYTES / pointSizeBytes;
			m_usableBytesPerBuffer = pointsPerInputBuffer * pointSizeBytes;
		}

		public PointCloudBinarySourceEnumerator GetBlockEnumerator(byte[] buffer)
		{
			return new PointCloudBinarySourceEnumerator(this, buffer);
		}
	}
}
