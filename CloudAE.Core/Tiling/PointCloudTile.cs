using System;

using Jacere.Core;
using Jacere.Core.Geometry;

namespace CloudAE.Core
{
	public class PointCloudTile : IProgress
	{
		public readonly PointCloudTileSet TileSet;

		public readonly ushort Row;
		public readonly ushort Col;
		public readonly long PointOffset;
		public readonly int PointCount;

		public readonly int ValidIndex;


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

		public UQuantizedExtent3D QuantizedExtent
		{
			get
			{
#warning store quantized extent in tileset so that TileSource isn't necessary
				return TileSet.ComputeTileExtent(this, TileSet.TileSource.QuantizedExtent);
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

		public PointCloudTile(PointCloudTileSet tileSet, ushort col, ushort row, int validIndex, long offset, int count)
		{
			if (count == 0)
				throw new ArgumentException("count");

			TileSet = tileSet;

			Row = row;
			Col = col;
			PointCount = count;

			PointOffset = offset;
			ValidIndex = validIndex;
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
			long position = TileSet.TileSource.PointDataOffset + (PointOffset * TileSet.TileSource.PointSizeBytes);
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
