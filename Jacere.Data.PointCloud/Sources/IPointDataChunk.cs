using System;
using System.Collections;
using System.Collections.Generic;
using Jacere.Core;
using Jacere.Core.Geometry;

namespace Jacere.Data.PointCloud
{
	public interface IPointDataChunk
	{
		int Index { get; }
		byte[] Data { get; }
		unsafe byte* PointDataPtr { get; }
		unsafe byte* PointDataEndPtr { get; }
		int Length { get; }
		short PointSizeBytes { get; }
		int PointCount { get; }

		IPointDataChunk CreateSegment(int pointCount);
	}

	public interface IPointDataProgressChunk : IPointDataChunk, IProgress
	{
	}

	public unsafe class SQuantizedPoint3DPtr
	{
		private readonly SQuantizedPoint3D* m_value;

		public SQuantizedPoint3DPtr(SQuantizedPoint3D* value)
		{
			m_value = value;
		}

		public SQuantizedPoint3D* GetPointer()
		{
			return m_value;
		}
	}

	public static class ChunkExtensions
	{
		public static SQuantizedPoint3DEnumerator GetSQuantizedPoint3DEnumerator(this IPointDataChunk chunk)
		{
			return new SQuantizedPoint3DEnumerator(chunk);
		}
	}

	public unsafe class SQuantizedPoint3DIterator
	{
		private readonly IPointDataChunk m_chunk;
		private byte* m_p;

		public bool IsValid
		{
			get { return (m_p != m_chunk.PointDataEndPtr); }
		}

		public SQuantizedPoint3DIterator(IPointDataChunk chunk)
		{
			m_chunk = chunk;
			m_p = m_chunk.PointDataPtr;
		}

		public SQuantizedPoint3D* Next()
		{
			var p = (SQuantizedPoint3D*)m_p;
			m_p += m_chunk.PointSizeBytes;
			return p;
		}
	}

	public unsafe class SQuantizedPoint3DEnumerator : IEnumerator<SQuantizedPoint3DPtr>, IEnumerable<SQuantizedPoint3DPtr>
	{
		private readonly IPointDataChunk m_chunk;
		private byte* m_p;

		public SQuantizedPoint3DEnumerator(IPointDataChunk chunk)
		{
			m_chunk = chunk;
			Reset();
		}

		#region IDisposable Implementation

		public void Dispose()
		{
		}

		#endregion

		#region IEnumerator Implementation

		public bool MoveNext()
		{
			m_p += m_chunk.PointSizeBytes;
			return (m_p < m_chunk.PointDataEndPtr);
		}

		public void Reset()
		{
			m_p = m_chunk.PointDataPtr;
		}

		public SQuantizedPoint3DPtr Current
		{
			get { return new SQuantizedPoint3DPtr((SQuantizedPoint3D*)m_p); }
		}

		object IEnumerator.Current
		{
			get { return Current; }
		}

		#endregion

		#region IEnumerable Implementation

		public IEnumerator<SQuantizedPoint3DPtr> GetEnumerator()
		{
			return this;
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		#endregion
	}
}
