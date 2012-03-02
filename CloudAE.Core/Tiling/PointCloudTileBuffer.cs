using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

using CloudAE.Core.Geometry;
using System.Runtime.InteropServices;

namespace CloudAE.Core
{
	unsafe class PointCloudTileBuffer
	{
		public readonly ushort Col;
		public readonly ushort Row;

		private readonly int m_pointOffset;
		private readonly int m_pointCount;

		private readonly IPointCloudTileBufferManager m_manager;

		private byte[] m_buffer;
		private int m_pointsWritten;
		private int m_currentPointIndex;

		private GCHandle m_gcHandle;
		private UQuantizedPoint3D* m_pBuffer;
		private UQuantizedPoint3D* m_pBufferEnd;

		private uint m_minX;
		private uint m_minY;
		private uint m_minZ;
		private uint m_maxX;
		private uint m_maxY;
		private uint m_maxZ;

		public PointCloudTileBuffer(PointCloudTile tile, IPointCloudTileBufferManager manager)
		{
			Col = tile.Col;
			Row = tile.Row;

			m_pointOffset = tile.PointOffset;
			m_pointCount = tile.PointCount;

			m_manager = manager;

			m_pointsWritten = 0;
			m_currentPointIndex = 0;

			UQuantizedExtent3D computedExtent = tile.QuantizedExtent;
			m_minX = computedExtent.MinX;
			m_minY = computedExtent.MinY;
			m_maxX = computedExtent.MaxX;
			m_maxY = computedExtent.MaxY;

			if (m_pointCount == 0)
			{
				m_minZ = computedExtent.MinZ;
				m_maxZ = computedExtent.MaxZ;
			}
			else
			{
				m_minZ = uint.MaxValue;
				m_maxZ = uint.MinValue;
			}
		}

		public void PinBuffer(byte[] buffer)
		{
			UnpinBuffer();
			m_gcHandle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
			IntPtr pAddr = Marshal.UnsafeAddrOfPinnedArrayElement(buffer, 0);
			m_pBuffer = (UQuantizedPoint3D*)pAddr.ToPointer();
			m_pBufferEnd = m_pBuffer + (buffer.Length / m_manager.TileSource.PointSizeBytes);
		}

		public void UnpinBuffer()
		{
			m_pBuffer = null;
			m_pBufferEnd = null;
			if (m_gcHandle.IsAllocated)
				m_gcHandle.Free();
		}

		/// <summary>
		/// Adds the point.
		/// </summary>
		/// <param name="point">The point.</param>
		/// <returns>true if the buffer is full or the tile is complete; otherwise false</returns>
		public unsafe bool AddPoint(UQuantizedPoint3D point)
		{
			if (m_buffer == null)
				throw new Exception("cannot add to inactive buffer");

			if (point.X < m_minX) m_minX = point.X; else if (point.X > m_maxX) m_maxX = point.X;
			if (point.Y < m_minY) m_minY = point.Y; else if (point.Y > m_maxY) m_maxY = point.Y;
			if (point.Z < m_minZ) m_minZ = point.Z; else if (point.Z > m_maxZ) m_maxZ = point.Z;

			(*m_pBuffer) = point;

			++m_currentPointIndex;
			++m_pBuffer;

			// buffer is full
			if (m_pBuffer == m_pBufferEnd)
				return true;

			// tile is complete
			if (m_pointsWritten + m_currentPointIndex == m_pointCount)
				return true;

			return false;
		}

		public void ActivateBuffer(byte[] buffer)
		{
			if (m_buffer != null)
				throw new Exception("buffer already activated");

			m_buffer = buffer;
			PinBuffer(m_buffer);
		}

		public byte[] DeactivateBuffer()
		{
			UnpinBuffer();
			byte[] buffer = m_buffer;
			m_buffer = null;

			return buffer;
		}

		public void Flush(FileStream outputStream)
		{
			if (m_buffer == null)
				throw new Exception("cannot flush inactive buffer");

			long byteOffset = ((long)m_pointOffset + m_pointsWritten) * m_manager.TileSource.PointSizeBytes;
			int bytesToWrite = m_currentPointIndex * m_manager.TileSource.PointSizeBytes;

			WriteQuantized(outputStream, byteOffset, m_buffer, bytesToWrite);

			m_pointsWritten += m_currentPointIndex;
			m_currentPointIndex = 0;
		}

		private unsafe void WriteQuantized(FileStream outputStream, long byteOffset, byte[] pointBuffer, int bytesToWrite)
		{
			long offset = m_manager.TileSource.PointDataOffset + byteOffset;
			if (outputStream.Position != offset)
				outputStream.Seek(offset, SeekOrigin.Begin);

			int bytesRemaining = bytesToWrite;

			while (bytesRemaining > 0)
			{
				int bytesInThisChunk = Math.Min(BufferManager.BUFFER_SIZE_BYTES, bytesRemaining);
				outputStream.Write(pointBuffer, bytesToWrite - bytesRemaining, bytesInThisChunk);
				bytesRemaining -= bytesInThisChunk;
			}
		}

		public UQuantizedExtent3D GetExtent()
		{
			return new UQuantizedExtent3D(m_minX, m_minY, m_minZ, m_maxX, m_maxY, m_maxZ);
		}

		/// <summary>
		/// Returns a <see cref="System.String"/> that represents this instance.
		/// </summary>
		/// <returns>
		/// A <see cref="System.String"/> that represents this instance.
		/// </returns>
		public override string ToString()
		{
			return String.Format("TileBuffer: [{0},{1}] {2}/{3}/{4}", Col, Row, m_pointsWritten, m_currentPointIndex, m_pointCount);
		}
	}
}
