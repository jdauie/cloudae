using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.IO;

using Jacere.Core;
using Jacere.Core.Geometry;
using Jacere.Data.PointCloud;

namespace CloudAE.Core
{
	public class PointCloudTileSet : IEnumerable<PointCloudTile>, ISerializeBinary, IQuantizedExtentGrid
	{
		private PointCloudTileSource m_tileSource;

		private readonly PointCloudTile[] m_tiles;
		private readonly Dictionary<int, int> m_tileIndex;

		public readonly Extent3D Extent;
		public readonly SQuantization3D Quantization;
		public readonly SQuantizedExtent3D QuantizedExtent;
		public readonly PointCloudTileDensity Density;
		public readonly long PointCount;
		public readonly int LowResCount;
		public readonly int TileCount;
		public readonly ushort Rows;
		public readonly ushort Cols;
		//public readonly double TileSize;
		public readonly int TileSizeX;
		public readonly int TileSizeY;

		public readonly int ValidTileCount;

		public int CellSizeX
		{
			get { return TileSizeX; }
		}

		public int CellSizeY
		{
			get { return TileSizeY; }
		}

		public PointCloudTileSource TileSource
		{
			get { return m_tileSource; }
			set { m_tileSource = value; }
		}

		private static Dictionary<int, int> CreateTileIndex(int validTileCount)
		{
			var tileMap = new Dictionary<int, int>(validTileCount);
			return tileMap;
		}

		public PointCloudTileSet(IPointCloudBinarySource source, PointCloudTileDensity density, SQuantizedExtentGrid<int> tileCounts, Grid<int> lowResCounts)
		{
			Extent = source.Extent;
			Quantization = source.Quantization;
			QuantizedExtent = source.QuantizedExtent;
			Density = density;

			Cols = tileCounts.SizeX;
			Rows = tileCounts.SizeY;
			TileSizeX = tileCounts.CellSizeX;
			TileSizeY = tileCounts.CellSizeY;
			//TileSize = tileCounts.CellSize;

			PointCount = density.PointCount;
			LowResCount = 0;
			TileCount = density.TileCount;
			ValidTileCount = density.ValidTileCount;

			m_tileIndex = CreateTileIndex(ValidTileCount);
			m_tiles = new PointCloudTile[density.ValidTileCount];

			// create valid tiles (in order)
			long offset = 0;
			int validTileIndex = 0;
			foreach (var tile in GetTileOrdering(Rows, Cols))
			{
				int pointCount = tileCounts.Data[tile.Row, tile.Col];
				if (pointCount > 0)
				{
					var lowResCount = lowResCounts.Data[tile.Row, tile.Col];
					LowResCount += lowResCount;
					m_tiles[validTileIndex] = new PointCloudTile(this, tile.Col, tile.Row, validTileIndex, offset, pointCount, lowResCount);
					m_tileIndex.Add(tile.Index, validTileIndex);
					++validTileIndex;
					offset += (pointCount - lowResCount);
				}
			}
		}

		public static IEnumerable<PointCloudTileCoord> GetTileOrdering(IGrid grid)
		{
			return GetTileOrdering(grid.SizeY, grid.SizeX);
		}

		public static IEnumerable<PointCloudTileCoord> GetTileOrdering(ushort rows, ushort cols)
		{
            for (ushort y = 0; y < rows; y++)
                for (ushort x = 0; x < cols; x++)
                    yield return new PointCloudTileCoord(y, x);
		}

		#region Serialization

		public PointCloudTileSet(BinaryReader reader)
		{
			Rows = reader.ReadUInt16();
			Cols = reader.ReadUInt16();
			TileSizeY = reader.ReadInt32();
			TileSizeX = reader.ReadInt32();

			TileCount = Rows * Cols;

			Extent = reader.ReadExtent3D();
			Quantization = reader.ReadSQuantization3D();
			QuantizedExtent = Quantization.Convert(Extent);
			Density = reader.ReadTileDensity();
			PointCount = Density.PointCount;
			ValidTileCount = Density.ValidTileCount;

			m_tileIndex = CreateTileIndex(ValidTileCount);
			m_tiles = new PointCloudTile[ValidTileCount];

			// fill in valid tiles (dense)
			long pointOffset = 0;
			var i = 0;
			foreach(var tile in GetTileOrdering(Rows, Cols))
			{
				var pointCount = reader.ReadInt32();
				var lowResCount = reader.ReadInt32();
				if (pointCount > 0)
				{
					m_tiles[i] = new PointCloudTile(this, tile.Col, tile.Row, i, pointOffset, pointCount, lowResCount);
					m_tileIndex.Add(tile.Index, i);

					pointOffset += (pointCount - lowResCount);
					++i;
				}
			}
		}

		public void Serialize(BinaryWriter writer)
		{
			writer.Write(Rows);
			writer.Write(Cols);
			writer.Write(TileSizeY);
			writer.Write(TileSizeX);
			writer.Write(Extent);
			writer.Write(Quantization);
			writer.Write(Density);

			// dense
			foreach (var tileCoord in GetTileOrdering(Rows, Cols))
			{
				var tile = GetTile(tileCoord);
				var pointCount = (tile != null) ? tile.PointCount : 0;
				var lowResCount = (tile != null) ? tile.LowResCount : 0;

				writer.Write(pointCount);
				writer.Write(lowResCount);
			}
		}

        #endregion

		public PointCloudTile GetTile(int row, int col)
		{
			return GetTileInternal(row, col);
		}

		public PointCloudTile GetTile(PointCloudTile tile)
		{
			return GetTileInternal(tile.Row, tile.Col);
		}

		private PointCloudTile GetTile(PointCloudTileCoord tile)
		{
			return GetTileInternal(tile.Row, tile.Col);
		}

		private PointCloudTile GetTileInternal(int row, int col)
		{
			var index = -1;
			return (m_tileIndex.TryGetValue(PointCloudTileCoord.GetIndex(row, col), out index) ? m_tiles[index] : null);
		}

		public IEnumerable<PointCloudTile> GetTileReadOrder(IEnumerable<PointCloudTile> tiles)
		{
			return tiles.OrderBy(t => t.PointOffset).ToArray();
		}

		public PointCloudTile GetTileByRatio(double xRatio, double yRatio)
		{
			var tileX = (int)(xRatio * Cols);
			if (tileX >= Cols) tileX = Cols - 1;

			var tileY = (int)(yRatio * Rows);
			if (tileY >= Rows) tileY = Rows - 1;

			return GetTile(tileY, tileX);
		}

		public Extent3D ComputeTileExtent(PointCloudTile tile)
		{
			var quantizedTileExtent = ComputeQuantizedTileExtent(tile);
			return Quantization.Convert(quantizedTileExtent);
		}

		public SQuantizedExtent3D ComputeQuantizedTileExtent(PointCloudTile tile)
		{
			return QuantizedExtent.ComputeQuantizedTileExtent(tile, this);
		}

		#region IEnumerable Members

		public IEnumerator<PointCloudTile> GetEnumerator()
		{
			return (m_tiles as IEnumerable<PointCloudTile>).GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		#endregion

		/// <summary>
		/// Returns a <see cref="System.String"/> that represents this instance.
		/// </summary>
		/// <returns>
		/// A <see cref="System.String"/> that represents this instance.
		/// </returns>
		public override string ToString()
		{
			return String.Format("[{0}x{1}] {2}", Cols, Rows, PointCount);
		}
	}
}
