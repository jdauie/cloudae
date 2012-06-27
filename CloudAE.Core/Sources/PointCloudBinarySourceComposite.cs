﻿using System;
using System.Collections.Generic;
using System.Linq;

using CloudAE.Core.Geometry;

namespace CloudAE.Core
{
	public class PointCloudBinarySourceComposite : PointCloudSource, IPointCloudBinarySourceEnumerable
	{
		private readonly PointCloudBinarySource[] m_sources;

		private readonly long m_count;
		private readonly Quantization3D m_quantization;
		private readonly short m_pointSizeBytes;
		private readonly int m_pointsPerBuffer;
		private readonly int m_usableBytesPerBuffer;

		private Extent3D m_extent;

		#region Properties

		public long Count
		{
			get { return m_count; }
		}

		public long PointDataOffset
		{
			get { throw new NotImplementedException(); }
		}

		public Quantization3D Quantization
		{
			get { return m_quantization; }
		}

		public short PointSizeBytes
		{
			get { return m_pointSizeBytes; }
		}

		public int UsableBytesPerBuffer
		{
			get { return m_usableBytesPerBuffer; }
		}

		public int PointsPerBuffer
		{
			get { return m_pointsPerBuffer; }
		}

		public Extent3D Extent
		{
			get { return m_extent; }
			protected set { m_extent = value; }
		}

		#endregion

		public PointCloudBinarySourceComposite(PointCloudBinarySource[] sources)
			: base("unknown")
		{
			m_sources = sources;

			// verify that they are compatible

			m_count = m_sources.Sum(s => s.Count);
			Extent = m_sources.Select(s => s.Extent).Union3D();
			m_quantization = m_sources[0].Quantization;
			m_pointSizeBytes = m_sources[0].PointSizeBytes;
			m_pointsPerBuffer = m_sources[0].PointsPerBuffer;
			m_usableBytesPerBuffer = m_sources[0].UsableBytesPerBuffer;
		}

		public IPointCloudBinarySourceEnumerator GetBlockEnumerator(BufferInstance buffer)
		{
			throw new NotImplementedException();
		}

		public IPointCloudBinarySourceEnumerator GetBlockEnumerator(ProgressManagerProcess process)
		{
			return new PointCloudBinarySourceCompositeEnumerator(m_sources, process);
		}
	}
}