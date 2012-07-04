using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.IO;

using CloudAE.Core.Geometry;

namespace CloudAE.Core
{
	public class PointCloudTileSet : IEnumerable<PointCloudTile>, ISerializeBinary
	{
		private readonly PointCloudTile[,] m_tiles;
		private readonly PointCloudTileTree m_tree;

		public readonly Extent3D Extent;
		public readonly PointCloudTileDensity Density;
		public readonly long PointCount;
		public readonly int TileCount;
		public readonly ushort Rows;
		public readonly ushort Cols;

		public readonly int ValidTileCount;

		/// <summary>
		/// Gets or sets the <see cref="CloudAE.Core.PointCloudTile"/> with the specified indices.
		/// </summary>
		/// <value></value>
		public PointCloudTile this[int x, int y]
		{
			get { return m_tiles[x, y]; }
			set { m_tiles[x, y] = value; }
		}

		public IEnumerable<PointCloudTile> ValidTiles
		{
			get { return m_tree.Select(t => m_tiles[t.Col, t.Row]).Where(t => t.IsValid); }
		}

		public PointCloudTileSet(PointCloudTileDensity density, Grid<int> tileCounts)
		{
			Extent = density.Extent;
			Density = density;

			Cols = tileCounts.SizeX;
			Rows = tileCounts.SizeY;

			PointCount = density.PointCount;
			TileCount = density.TileCount;
			ValidTileCount = density.ValidTileCount;

			// create tile ordering
			m_tree = new PointCloudTileTree(Cols, Rows);

			// create empty tile grid
			m_tiles = new PointCloudTile[Cols, Rows];
			for (ushort x = 0; x < Cols; x++)
				for (ushort y = 0; y < Rows; y++)
					m_tiles[x, y] = new PointCloudTile(x, y, 0, 0, 0);

			// create valid tiles (in order)
			long offset = 0;
			int validTileIndex = 0;
			foreach (var tile in m_tree)
			{
				int count = tileCounts.Data[tile.Col, tile.Row];
				m_tiles[tile.Col, tile.Row] = new PointCloudTile(tile.Col, tile.Row, validTileIndex, offset, count);

				if (count > 0)
				{
					++validTileIndex;
					offset += count;
				}
			}
		}

		public PointCloudTileSet(PointCloudTileSet tileSet, PointCloudTileSource tileSource)
		{
			Extent = tileSet.Extent;
			Density = tileSet.Density;

			PointCount = tileSet.PointCount;
			TileCount = tileSet.TileCount;
			ValidTileCount = tileSet.ValidTileCount;

			Cols = tileSet.Cols;
			Rows = tileSet.Rows;

			m_tree = tileSet.m_tree;
			m_tiles = new PointCloudTile[Cols, Rows];
			foreach (var tile in tileSet)
				m_tiles[tile.Col, tile.Row] = new PointCloudTile(tileSet[tile.Col, tile.Row], tileSource);
		}

		public PointCloudTileSet(PointCloudTileSet[] tileSets)
			: this(tileSets[0], null)
		{
			if (tileSets.Length < 2)
				throw new ArgumentException("There must be at least two tile sets to merge.", "tileSets");

			for (int i = 1; i < tileSets.Length; i++)
			{
				if (!(
					tileSets[i].Rows == tileSets[0].Rows &&
					tileSets[i].Cols == tileSets[0].Cols &&
					tileSets[i].Extent.Equals(tileSets[0].Extent)
					))
				{
					throw new ArgumentException("Tile sets cannot be merged.", "tileSets");
				}
			}

			PointCount = tileSets.Sum(t => t.PointCount);

			long offset = 0;
			int validTileIndex = 0;
			foreach (var tile in m_tree)
			{
				int tileCount = tileSets.Sum(t => t.m_tiles[tile.Col, tile.Row].PointCount);

				m_tiles[tile.Col, tile.Row] = new PointCloudTile(tile.Col, tile.Row, validTileIndex, offset, tileCount);

				if (tileCount > 0)
				{
					++validTileIndex;
					offset += tileCount;
				}
			}

			Density = new PointCloudTileDensity(TileCount, this.Select(t => t.PointCount), Extent);
			ValidTileCount = Density.ValidTileCount;
		}

		#region Serialization

		public PointCloudTileSet(BinaryReader reader)
		{
			Rows = reader.ReadUInt16();
			Cols = reader.ReadUInt16();

			TileCount = Rows * Cols;

			Extent = reader.ReadExtent3D();
			Density = reader.ReadTileDensity();
			PointCount = Density.PointCount;
			ValidTileCount = Density.ValidTileCount;

			// create tile ordering
			m_tree = new PointCloudTileTree(Cols, Rows);

			// create empty tile grid
			m_tiles = new PointCloudTile[Cols, Rows];
			for (ushort x = 0; x < Cols; x++)
				for (ushort y = 0; y < Rows; y++)
					m_tiles[x, y] = new PointCloudTile(x, y, 0, 0, 0);

			// fill in valid tiles
			long pointOffset = 0;
			for (int i = 0; i < ValidTileCount; i++)
			{
				ushort x = reader.ReadUInt16();
				ushort y = reader.ReadUInt16();
				int count = reader.ReadInt32();

				m_tiles[x, y] = new PointCloudTile(x, y, i, pointOffset, count);

				pointOffset += count;
			}
		}

		public void Serialize(BinaryWriter writer)
		{
			writer.Write(Rows);
			writer.Write(Cols);
			writer.Write(Extent);
			writer.Write(Density);

			foreach (var tile in ValidTiles)
			{
				writer.Write(tile.Col);
				writer.Write(tile.Row);
				writer.Write(tile.PointCount);
			}
		}

		#endregion

		public PointCloudTileBufferPosition[,] CreatePositionGrid(PointBufferWrapper segmentBuffer)
		{
			// create tile position counters
			var tilePositions = new PointCloudTileBufferPosition[Cols + 1, Rows + 1];
			{
				foreach (PointCloudTile tile in ValidTiles)
					tilePositions[tile.Col, tile.Row] = new PointCloudTileBufferPosition(segmentBuffer, tile);

				// buffer the edges for overflow
				for (int x = 0; x < Cols; x++)
					tilePositions[x, Rows] = tilePositions[x, Rows - 1];
				for (int y = 0; y <= Rows; y++)
					tilePositions[Cols, y] = tilePositions[Cols - 1, y];
			}

			return tilePositions;
		}

		public PointCloudTile GetTileByRatio(double xRatio, double yRatio)
		{
			int tileX = (int)(xRatio * Cols);
			if (tileX >= Cols) tileX = Cols - 1;

			int tileY = (int)(yRatio * Rows);
			if (tileY >= Rows) tileY = Rows - 1;

			return m_tiles[tileX, tileY];
		}

		public Extent3D ComputeTileExtent(PointCloudTile tile)
		{
			double rangeX = Extent.RangeX / Cols;
			double rangeY = Extent.RangeY / Rows;

			double minX = rangeX * tile.Col + Extent.MinX;
			double minY = rangeY * tile.Row + Extent.MinY;
			double minZ = Extent.MinZ;
			double maxX = Math.Min(minX + rangeX, Extent.MaxX);
			double maxY = Math.Min(minY + rangeY, Extent.MaxY);
			double maxZ = Extent.MaxZ;

			return new Extent3D(minX, minY, minZ, maxX, maxY, maxZ);
		}

		public UQuantizedExtent3D ComputeTileExtent(PointCloudTile tile, UQuantizedExtent3D extent)
		{
			double rangeX = (double)extent.RangeX / Cols;
			double rangeY = (double)extent.RangeY / Rows;

			uint minX = (uint)(rangeX * tile.Col + extent.MinX);
			uint minY = (uint)(rangeY * tile.Row + extent.MinY);
			uint minZ = extent.MinZ;
			uint maxX = (uint)(Math.Min(minX + rangeX, extent.MaxX));
			uint maxY = (uint)(Math.Min(minY + rangeY, extent.MaxY));
			uint maxZ = extent.MaxZ;

			return new UQuantizedExtent3D(minX, minY, minZ, maxX, maxY, maxZ);
		}

		#region IEnumerable Members

		public IEnumerator<PointCloudTile> GetEnumerator()
		{
			// traverse the tiles in storage order
			return m_tree.Select(tile => m_tiles[tile.Col, tile.Row]).GetEnumerator();
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
