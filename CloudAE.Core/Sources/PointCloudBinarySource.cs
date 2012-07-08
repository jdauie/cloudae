using System;
using System.Collections.Generic;
using System.Linq;

using CloudAE.Core.Geometry;

namespace CloudAE.Core
{
	public class PointCloudBinarySource : PointCloudSource, IPointCloudBinarySource
	{
		public const string FILE_EXTENSION = "bin";

		private readonly long m_count;
		private readonly Quantization3D m_quantization;
		private readonly short m_pointSizeBytes;

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

		public short PointSizeBytes
		{
			get { return m_pointSizeBytes; }
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

		public PointCloudBinarySource(string file, long count, Extent3D extent, Quantization3D quantization, long dataOffset, short pointSizeBytes)
			: base(file)
		{
			m_count = count;
			Extent = extent;
			m_quantization = quantization;
			PointDataOffset = dataOffset;
			m_pointSizeBytes = pointSizeBytes;
		}

		public IPointCloudBinarySourceEnumerator GetBlockEnumerator(ProgressManagerProcess process)
		{
			return new PointCloudBinarySourceEnumerator(this, process);
		}

		public IPointCloudBinarySourceEnumerator GetBlockEnumerator(BufferInstance buffer)
		{
			return new PointCloudBinarySourceEnumerator(this, buffer);
		}
	}
}
