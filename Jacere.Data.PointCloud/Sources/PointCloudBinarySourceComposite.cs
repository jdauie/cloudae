using System;
using System.Collections.Generic;
using System.Linq;
using Jacere.Core;
using Jacere.Core.Geometry;

namespace Jacere.Data.PointCloud
{
	/// <summary>
	/// 
	/// </summary>
	public sealed class PointCloudBinarySourceComposite : PointCloudSource, IPointCloudBinarySource
	{
		private readonly IPointCloudBinarySource[] m_sources;

		private readonly long m_count;
		private readonly SQuantization3D m_quantization;
		private readonly short m_pointSizeBytes;

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

		public SQuantization3D Quantization
		{
			get { return m_quantization; }
		}

		public short PointSizeBytes
		{
			get { return m_pointSizeBytes; }
		}

		public Extent3D Extent
		{
			get { return m_extent; }
			protected set { m_extent = value; }
		}

		public IEnumerable<string> SourcePaths
		{
			get { return m_sources.Select(f => f.FilePath); }
		}

		#endregion

		public PointCloudBinarySourceComposite(FileHandlerBase file, Extent3D extent, IPointCloudBinarySource[] sources)
			: base(file)
		{
			m_sources = sources;

			// verify that they are compatible

			m_count = m_sources.Sum(s => s.Count);
			Extent = extent;
			m_quantization = m_sources[0].Quantization;
			m_pointSizeBytes = m_sources[0].PointSizeBytes;
		}

		public IStreamReader GetStreamReader()
		{
			// should never be called
			throw new NotImplementedException();
		}

		public IPointCloudBinarySourceEnumerator GetBlockEnumerator(BufferInstance buffer)
		{
			return new PointCloudBinarySourceCompositeEnumerator(m_sources, buffer);
		}

		public IPointCloudBinarySourceEnumerator GetBlockEnumerator(ProgressManagerProcess process)
		{
			return new PointCloudBinarySourceCompositeEnumerator(m_sources, process);
		}

		public IPointCloudBinarySource CreateSegment(long pointIndex, long pointCount)
		{
			var subset = CreateSegmentSources(pointIndex, pointCount);

			// this will break extents, etc.
			//if (subset.Count == 1)
			//    return subset[0];

			var composite = new PointCloudBinarySourceComposite(FileHandler, Extent, subset.ToArray());
			return composite;
		}

		private List<IPointCloudBinarySource> CreateSegmentSources(long pointIndex, long pointCount)
		{
			// make a new set of binary sources
			var subset = new List<IPointCloudBinarySource>(m_sources.Length);

			long currentIndex = 0;
			long pointsRemaining = pointCount;
			foreach (var source in m_sources)
			{
				if (pointsRemaining == 0)
					break;

				if (subset.Count == 0 && currentIndex + source.Count <= pointIndex)
				{
					currentIndex += source.Count;
					continue;
				}

				// add this as a partial (it may need both ends adjusted)
				long segmentStart = subset.Count == 0 ? pointIndex - currentIndex : 0;
				long segmentLength = Math.Min(pointsRemaining, source.Count - segmentStart);
				var segment = source.CreateSegment(segmentStart, segmentLength);
				subset.Add(segment);

				pointsRemaining -= segmentLength;
			}
			return subset;
		}

		public IPointCloudBinarySource CreateSparseSegment(PointCloudBinarySourceEnumeratorSparseRegion regions)
		{
			// break into segments that do not span files
			var regionSegments = new List<IPointCloudBinarySource>();
			foreach (var region in regions)
			{
				long pointIndex = regions.PointsPerChunk * region.ChunkStart;
				long pointCount = regions.PointsPerChunk * region.ChunkCount;
				var regionSegmentSources = CreateSegmentSources(pointIndex, pointCount);
				regionSegments.AddRange(regionSegmentSources);
			}

			var sparseComposite = new PointCloudBinarySourceComposite(FileHandler, Extent, regionSegments.ToArray());

			return sparseComposite;
		}
	}
}
