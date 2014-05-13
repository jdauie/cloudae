using System;

using Jacere.Core;
using Jacere.Core.Geometry;

namespace CloudAE.Core
{
	public class PointCloudTile : IProgress, IGridCoord
	{
		public readonly PointCloudTileSet TileSet;

		private readonly ushort m_row;
		private readonly ushort m_col;

		public readonly long PointOffset;
		public readonly int PointCount;
		public readonly int LowResOffset;
		public readonly int LowResCount;

		public readonly int ValidIndex;

		public ushort Row
		{
			get { return m_row; }
		}

		public ushort Col
		{
			get { return m_col; }
		}

		public int StorageSize
		{
			get
			{
#warning cache point size somewhere to TileSource isn't necessary (also used in ReadTile, and other externals)
				return PointCount * TileSet.TileSource.PointSizeBytes;
			}
		}

		public Extent3D Extent
		{
			get
			{
				return TileSet.ComputeTileExtent(this);
			}
		}

		public SQuantizedExtent3D QuantizedExtent
		{
			get
			{
				return TileSet.ComputeQuantizedTileExtent(this);
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
				return (float)ValidIndex / TileSet.ValidTileCount;
			}
		}

		public PointCloudTile(PointCloudTileSet tileSet, ushort col, ushort row, int validIndex, long offset, int count, int lowResOffset, int lowResCount)
		{
			if (count == 0)
				throw new ArgumentException("count");

			TileSet = tileSet;

			m_row = row;
			m_col = col;
			PointCount = count;
			LowResOffset = lowResOffset;
			LowResCount = lowResCount;

			PointOffset = offset;
			ValidIndex = validIndex;
		}

		public int ReadTile(IStreamReader inputStream, byte[] inputBuffer)
		{
			return ReadTile(inputStream, inputBuffer, 0);
		}

		public int ReadTile(IStreamReader inputStream, byte[] inputBuffer, int index)
		{
			if (PointCount == 0)
				return 0;

			if (index + StorageSize > inputBuffer.Length)
				throw new ArgumentException("Tile data is larger than available buffer", "inputBuffer");

			// seek if necessary (hopefully this is minimized)
			var position = TileSet.TileSource.PointDataOffset + (PointOffset * TileSet.TileSource.PointSizeBytes);
			if (inputStream.Position != position)
				inputStream.Seek(position);
			
			// read available points from main tile area and get low-res points from source
			var localStorageSize = (PointCount - LowResCount) * TileSet.TileSource.PointSizeBytes;
			var bytesRead = inputStream.Read(inputBuffer, index, localStorageSize);

			//bytesRead += TileSet.TileSource.ReadLowResTile(this, inputBuffer, index + bytesRead);
			var lowResPosition = TileSet.TileSource.PointDataOffset + ((TileSet.PointCount - TileSet.LowResCount + LowResOffset) * TileSet.TileSource.PointSizeBytes);
			inputStream.Seek(lowResPosition);
			var lowResStorageSize = LowResCount * TileSet.TileSource.PointSizeBytes;
			bytesRead += inputStream.Read(inputBuffer, index + bytesRead, lowResStorageSize);

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
