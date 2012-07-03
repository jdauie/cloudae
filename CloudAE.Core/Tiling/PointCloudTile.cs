using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Diagnostics;

using CloudAE.Core.Geometry;

namespace CloudAE.Core
{
	/// <summary>
	/// This used to be a struct, but it is too large now -- performance was getting degraded.
	/// </summary>
	public class PointCloudTile : IProgress
	{
		public readonly PointCloudTileSource TileSource;

		public readonly ushort Row;
		public readonly ushort Col;
		public readonly int PointOffset;
		public readonly int PointCount;

		public readonly int StorageSize;

		public readonly int ValidIndex;

		private UQuantizedExtent3D m_quantizedExtent;
		private Extent3D m_extent;

		private float? m_progress;

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
		}

		public bool IsValid
		{
			get { return PointCount > 0; }
		}

		public float Progress
		{
			get
			{
				if (!m_progress.HasValue)
				{
					if (TileSource != null && TileSource.TileSet != null)
						m_progress = (float)ValidIndex / TileSource.TileSet.ValidTileCount;
				}

				return m_progress.Value;
			}
		}

		public PointCloudTile(ushort col, ushort row, int validIndex, int offset, int count)
		{
			Row = row;
			Col = col;
			PointOffset = offset;
			PointCount = count;

			StorageSize = 0;

			ValidIndex = validIndex;
		}

		public PointCloudTile(PointCloudTile tile, PointCloudTileSource tileSource)
		{
			TileSource = tileSource;

			Row = tile.Row;
			Col = tile.Col;
			PointOffset = tile.PointOffset;
			PointCount = tile.PointCount;

			ValidIndex = tile.ValidIndex;

			if (TileSource != null)
				StorageSize = PointCount * TileSource.PointSizeBytes;
		}

		public int ReadTile(IStreamReader inputStream, byte[] inputBuffer)
		{
			return ReadTile(inputStream, inputBuffer, 0);
		}

		public int ReadTile(IStreamReader inputStream, byte[] inputBuffer, int index)
		{
			if (StorageSize > inputBuffer.Length)
				throw new ArgumentException("Tile data is larger than available buffer", "inputBuffer");

			if (StorageSize == 0)
				return 0;

			// seek if necessary (hopefully this is minimized)
			long position = TileSource.PointDataOffset + PointOffset * TileSource.PointSizeBytes;
			if (inputStream.Position != position)
				inputStream.Seek(position);

			int bytesRead = inputStream.Read(inputBuffer, index, StorageSize);

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
			var tile = obj as PointCloudTile;
			if (tile != null)
				return (tile.Row == Row && tile.Col == Col);
			return false;
		}
	}
}
