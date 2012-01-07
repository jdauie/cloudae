using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Diagnostics;

using CloudAE.Core.Geometry;
using CloudAE.Core.Compression;

namespace CloudAE.Core
{
	public struct PointCloudTile
	{
		public readonly PointCloudTileSource TileSource;

		public readonly ushort Row;
		public readonly ushort Col;
		public readonly int PointOffset;
		public readonly int PointCount;

		public readonly long StorageOffset;
		public readonly int StorageSize;

		private uint m_minX;
		private uint m_minY;
		private uint m_minZ;
		private uint m_maxX;
		private uint m_maxY;
		private uint m_maxZ;

		public Extent3D Extent
		{
			get { return TileSource.Quantization.Convert(QuantizedExtent); }
		}

		public UQuantizedExtent3D QuantizedExtent
		{
			get { return new UQuantizedExtent3D(m_minX, m_minY, m_minZ, m_maxX, m_maxY, m_maxZ); }
		}

		public int Index
		{
			get { return Col * TileSource.TileSet.Rows + Row; }
		}

		public bool IsValid
		{
			get { return PointCount > 0; }
		}

		public PointCloudTile(ushort col, ushort row, int offset, int count, long storageOffset, int storageSize, UQuantizedExtent3D extent, PointCloudTileSource tileSource)
		{
			TileSource = tileSource;

			Row = row;
			Col = col;
			PointOffset = offset;
			PointCount = count;

			StorageOffset = storageOffset;
			StorageSize = storageSize;

			if (extent != null)
			{
				m_minX = extent.MinX;
				m_minY = extent.MinY;
				m_minZ = extent.MinZ;
				m_maxX = extent.MaxX;
				m_maxY = extent.MaxY;
				m_maxZ = extent.MaxZ;
			}
			else
			{
				m_minX = 0;
				m_minY = 0;
				m_minZ = 0;
				m_maxX = 0;
				m_maxY = 0;
				m_maxZ = 0;
			}
		}

		public PointCloudTile(PointCloudTile tile, PointCloudTileSource tileSource)
		{
			TileSource = tileSource;

			Row = tile.Row;
			Col = tile.Col;
			PointOffset = tile.PointOffset;
			PointCount = tile.PointCount;

			StorageOffset = tile.StorageOffset;
			StorageSize = tile.StorageSize;

			m_minX = tile.m_minX;
			m_minY = tile.m_minY;
			m_minZ = tile.m_minZ;
			m_maxX = tile.m_maxX;
			m_maxY = tile.m_maxY;
			m_maxZ = tile.m_maxZ;
		}

		public PointCloudTile(PointCloudTile tile)
			: this(tile, tile.TileSource)
		{
		}

		public PointCloudTile(PointCloudTile tile, long storageOffset, int storageSize)
			: this(tile)
		{
			StorageOffset = storageOffset;
			StorageSize = storageSize;
		}

		public PointCloudTile(PointCloudTile tile, UQuantizedExtent3D extent)
			: this(tile)
		{
			m_minX = extent.MinX;
			m_minY = extent.MinY;
			m_minZ = extent.MinZ;
			m_maxX = extent.MaxX;
			m_maxY = extent.MaxY;
			m_maxZ = extent.MaxZ;
		}

		public unsafe int ReadTile(FileStream inputStream, byte[] inputBuffer)
		{
			if (StorageSize > inputBuffer.Length)
				throw new ArgumentException("Tile data is larger than available buffer", "inputBuffer");

			if (StorageSize == 0)
				return 0;

			bool compressed = (TileSource != null && TileSource.Compression != CompressionMethod.None);
			bool actuallyCompressed = compressed && (StorageSize != PointCount * TileSource.PointSizeBytes);

			// seek if necessary (hopefully this is minimized)
			long position = TileSource.PointDataOffset + StorageOffset;
			if (inputStream.Position != position)
				inputStream.Seek(position, SeekOrigin.Begin);

			// if there is no compression, just read into the input buffer
			byte[] tempBuffer = inputBuffer;

			if (actuallyCompressed)
			{
				tempBuffer = BufferManager.AcquireBuffer();
			}

			int bytesRead = inputStream.Read(tempBuffer, 0, StorageSize);
			Debug.Assert(bytesRead == StorageSize);

			if (compressed)
			{
				if (actuallyCompressed)
				{
					// this should be done with discovery/factory
					switch (TileSource.Compression)
					{
						//case CompressionMethod.QuickLZ:
						//    bytesRead = QuickLZ.Decompress(tempBuffer, bytesRead, inputBuffer);
						//    break;

						//case CompressionMethod.DotNetZip:
						//    bytesRead = DotNetZip.Decompress(tempBuffer, bytesRead, inputBuffer);
						//    break;

						//case CompressionMethod.SevenZipSharp:
						//    bytesRead = SevenZipSharp.Decompress(tempBuffer, bytesRead, inputBuffer);
						//    break;
					}

					BufferManager.ReleaseBuffer(tempBuffer);
					tempBuffer = null;
				}

				// decode deltas and offsets
				fixed (byte* inputBufferPtr = inputBuffer)
				{
					UQuantizedPoint3D* p = (UQuantizedPoint3D*)inputBufferPtr;

					p[0].X += m_minX;
					p[0].Y += m_minY;
					p[0].Z += m_minZ;

					for (int i = 1; i < PointCount; i++)
					{
						p[i].X += m_minX;
						p[i].Y += m_minY;
						p[i].Z += p[i - 1].Z;
					}
				}
			}

			return bytesRead;
		}

		/// <summary>
		/// Returns a <see cref="System.String"/> that represents this instance.
		/// </summary>
		/// <returns>
		/// A <see cref="System.String"/> that represents this instance.
		/// </returns>
		public override string ToString()
		{
			return String.Format("Tile [{0},{1}] {2}", Col, Row, PointCount);
		}

		public override bool Equals(object obj)
		{
			if (obj is PointCloudTile)
			{
				PointCloudTile tile = (PointCloudTile)obj;
				return (tile.Row == Row && tile.Col == Col);
			}
			return false;
		}
	}
}
