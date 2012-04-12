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
	/// <summary>
	/// This used to be a struct, but it is too large now -- performance was getting degraded.
	/// </summary>
	public class PointCloudTile
	{
		public readonly PointCloudTileSource TileSource;

		public readonly ushort Row;
		public readonly ushort Col;
		public readonly int PointOffset;
		public readonly int PointCount;

		public readonly long StorageOffset;
		public readonly int StorageSize;

		public readonly int Index;
		public readonly int ValidIndex;

		private UQuantizedExtent3D m_quantizedExtent;
		private Extent3D m_extent;

		public Extent3D Extent
		{
			get
			{
				if(m_extent == null)
					m_extent = TileSource.Quantization.Convert(QuantizedExtent);

				return m_extent;
			}
		}

		public UQuantizedExtent3D QuantizedExtent
		{
			get
			{
				if (m_quantizedExtent == null)
				{
					m_quantizedExtent = TileSource.TileSet.ComputeTileExtent(this, TileSource.QuantizedExtent);
					m_extent = null;
				}

				return m_quantizedExtent;
			}
			set
			{
				m_quantizedExtent = value;
				m_extent = null;
			}
		}

		//public int Index
		//{
		//    get { return Col * TileSource.TileSet.Rows + Row; }
		//}

		public bool IsValid
		{
			get { return PointCount > 0; }
		}

		public PointCloudTile(ushort col, ushort row, int validIndex, int offset, int count, long storageOffset, int storageSize, UQuantizedExtent3D extent, PointCloudTileSource tileSource)
		{
			TileSource = tileSource;

			Row = row;
			Col = col;
			PointOffset = offset;
			PointCount = count;

			StorageOffset = storageOffset;
			StorageSize = storageSize;

			Index = (Col << 16) | Row;
			ValidIndex = validIndex;

			m_quantizedExtent = extent;
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

			Index = tile.Index;
			ValidIndex = tile.ValidIndex;

			m_quantizedExtent = tile.m_quantizedExtent;
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
					bytesRead = TileSource.Compressor.Decompress(this, tempBuffer, bytesRead, inputBuffer);

					BufferManager.ReleaseBuffer(tempBuffer);
					tempBuffer = null;
				}

				// decode deltas and offsets
				fixed (byte* inputBufferPtr = inputBuffer)
				{
					UQuantizedPoint3D* p = (UQuantizedPoint3D*)inputBufferPtr;

					p[0].X += m_quantizedExtent.MinX;
					p[0].Y += m_quantizedExtent.MinY;
					p[0].Z += m_quantizedExtent.MinZ;

					for (int i = 1; i < PointCount; i++)
					{
						p[i].X += p[i - 1].X;
						//p[i].X += m_quantizedExtent.MinX;
						p[i].Y += m_quantizedExtent.MinY;
						p[i].Z += m_quantizedExtent.MinZ;
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
