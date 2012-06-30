using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;

using System.Runtime.InteropServices;

namespace CloudAE.Core
{
	unsafe class PointCloudTileBuffer
	{
		public readonly ushort Col;
		public readonly ushort Row;

		private readonly int m_pointOffset;
		private readonly int m_pointCount;
		private readonly int m_pointSizeBytes;

		private readonly IPointCloudTileBufferManager m_manager;

		private byte[] m_buffer;
		private int m_pointsWritten;
		private int m_currentPointIndex;

		private GCHandle m_gcHandle;
		private byte* m_pBuffer;

		public PointCloudTileBuffer(PointCloudTile tile, IPointCloudTileBufferManager manager)
		{
			Col = tile.Col;
			Row = tile.Row;

			m_manager = manager;

			m_pointOffset = tile.PointOffset;
			m_pointCount = tile.PointCount;
			m_pointSizeBytes = m_manager.TileSource.PointSizeBytes;

			m_pointsWritten = 0;
			m_currentPointIndex = 0;
		}

		public void PinBuffer(byte[] buffer)
		{
			UnpinBuffer();
			m_gcHandle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
			IntPtr pAddr = Marshal.UnsafeAddrOfPinnedArrayElement(buffer, 0);
			m_pBuffer = (byte*)pAddr.ToPointer();
		}

		public void UnpinBuffer()
		{
			m_pBuffer = null;
			if (m_gcHandle.IsAllocated)
				m_gcHandle.Free();
		}

		/// <summary>
		/// Adds the point.
		/// </summary>
		/// <param name="p">The point.</param>
		/// <returns>true if the buffer is full or the tile is complete; otherwise false</returns>
		public void AddPoint(byte* p)
		{
			if (m_buffer == null)
				throw new Exception("cannot add to inactive buffer");

			for (int i = 0; i < m_pointSizeBytes; i++)
				m_pBuffer[i] = p[i];

			++m_currentPointIndex;
			m_pBuffer += m_pointSizeBytes;
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

		public void Flush(IStreamWriter outputStream)
		{
			if (m_buffer == null)
				throw new Exception("cannot flush inactive buffer");

			long byteOffset = ((long)m_pointOffset + m_pointsWritten) * m_manager.TileSource.PointSizeBytes;
			int bytesToWrite = m_currentPointIndex * m_manager.TileSource.PointSizeBytes;

			//WriteQuantized(outputStream, byteOffset, m_buffer, bytesToWrite);
			outputStream.Write(m_buffer, 0, bytesToWrite);

			m_pointsWritten += m_currentPointIndex;
			m_currentPointIndex = 0;
		}

		//private void WriteQuantized(IStreamWriter outputStream, long byteOffset, byte[] pointBuffer, int bytesToWrite)
		//{
		//    long offset = m_manager.TileSource.PointDataOffset + byteOffset;
		//    if (outputStream.Position != offset)
		//        outputStream.Seek(offset, SeekOrigin.Begin);

		//    int bytesRemaining = bytesToWrite;

		//    while (bytesRemaining > 0)
		//    {
		//        int bytesInThisChunk = Math.Min(BufferManager.BUFFER_SIZE_BYTES, bytesRemaining);
		//        outputStream.Write(pointBuffer, bytesToWrite - bytesRemaining, bytesInThisChunk);
		//        bytesRemaining -= bytesInThisChunk;
		//    }
		//}

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
