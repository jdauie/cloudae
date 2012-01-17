using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CloudAE.Core.Geometry;
using System.IO;

namespace CloudAE.Core
{
	public class PointCloudTileSet : IEnumerable<PointCloudTile>, ISerializeBinary
	{
		private readonly PointCloudTile[,] m_tiles;

		public readonly Extent3D Extent;
		public readonly PointCloudTileDensity Density;
		public readonly long PointCount;
		public readonly int TileCount;
		public readonly ushort Rows;
		public readonly ushort Cols;

		public readonly int ValidTileCount;

		/// <summary>
		/// Gets or sets the <see cref="CloudAE.Core.PointCloudTile"/> with the specified x.
		/// This is less than 10% slower than accessing the array directly.
		/// </summary>
		/// <value></value>
		public PointCloudTile this[int x, int y]
		{
			get { return m_tiles[x, y]; }
			set { m_tiles[x, y] = value; }
		}

		public IEnumerable<PointCloudTile> ValidTiles
		{
			get { return this.Where(t => t.IsValid); }
		}

		public PointCloudTileSet(PointCloudTileDensity density, Grid<int> tileCounts)
		{
			Extent = density.Extent;
			Density = density;

			Cols = tileCounts.SizeX;
			Rows = tileCounts.SizeY;

			TileCount = Rows * Cols;

			m_tiles = new PointCloudTile[Cols, Rows];
			int offset = 0;
			int validTileIndex = 0;
			for (ushort x = 0; x < Cols; x++)
			{
				for (ushort y = 0; y < Rows; y++)
				{
					int tileCount = tileCounts.Data[x, y];
					long byteOffset = (long)offset * BufferManager.QUANTIZED_POINT_SIZE_BYTES;
					int byteLength = tileCount * BufferManager.QUANTIZED_POINT_SIZE_BYTES;

					m_tiles[x, y] = new PointCloudTile(x, y, validTileIndex, offset, tileCount, byteOffset, byteLength, null, null);

					if (tileCount > 0)
					{
						++validTileIndex;
						offset += tileCount;
					}
				}
			}
			PointCount = offset;
			ValidTileCount = this.Where(t => t.IsValid).Count();
		}

		public PointCloudTileSet(PointCloudTileSet tileSet, PointCloudTileSource tileSource)
		{
			Extent = tileSet.Extent;
			Density = tileSet.Density;

			TileCount = tileSet.TileCount;

			Cols = tileSet.Cols;
			Rows = tileSet.Rows;

			PointCount = tileSet.PointCount;

			m_tiles = new PointCloudTile[Cols, Rows];
			for (ushort x = 0; x < Cols; x++)
			{
				for (ushort y = 0; y < Rows; y++)
				{
					PointCloudTile currentTile = tileSet.m_tiles[x, y];
					m_tiles[x, y] = new PointCloudTile(currentTile, tileSource);
				}
			}
			ValidTileCount = this.Where(t => t.IsValid).Count();
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

			int offset = 0;
			int validTileIndex = 0;
			for (ushort x = 0; x < Cols; x++)
			{
				for (ushort y = 0; y < Rows; y++)
				{
					int tileCount = tileSets.Sum(t => t.m_tiles[x, y].PointCount);

					long byteOffset = (long)offset * BufferManager.QUANTIZED_POINT_SIZE_BYTES;
					int byteLength = tileCount * BufferManager.QUANTIZED_POINT_SIZE_BYTES;

					UQuantizedExtent3D mergedExtent = tileSets.Select(s => s.m_tiles[x, y].QuantizedExtent).Union();
					m_tiles[x, y] = new PointCloudTile(x, y, validTileIndex, offset, tileCount, byteOffset, byteLength, mergedExtent, null);

					if (tileCount > 0)
					{
						++validTileIndex;
						offset += tileCount;
					}
				}
			}

			Density = new PointCloudTileDensity(TileCount, this.Select(t => t.PointCount), Extent);
		}

		#region Serialization

		public PointCloudTileSet(BinaryReader reader)
		{
			Rows = reader.ReadUInt16();
			Cols = reader.ReadUInt16();

			TileCount = Rows * Cols;

			Extent = reader.ReadExtent3D();
			Density = reader.ReadTileDensity();

			m_tiles = new PointCloudTile[Cols, Rows];
			int pointOffset = 0;
			long storageOffset = 0;
			int validTileIndex = 0;
			for (ushort x = 0; x < Cols; x++)
			{
				for (ushort y = 0; y < Rows; y++)
				{
					int tilePointCount = reader.ReadInt32();
					int tileStorageSize = reader.ReadInt32();
					UQuantizedExtent3D quantizedExtent = reader.ReadUQuantizedExtent3D();

					m_tiles[x, y] = new PointCloudTile(x, y, validTileIndex, pointOffset, tilePointCount, storageOffset, tileStorageSize, quantizedExtent, null);

					if (tilePointCount > 0)
					{
						++validTileIndex;
						pointOffset += tilePointCount;
						storageOffset += tileStorageSize;
					}
				}
			}
			PointCount = pointOffset;
			ValidTileCount = this.Where(t => t.IsValid).Count();
		}

		public void Serialize(BinaryWriter writer)
		{
			writer.Write(Rows);
			writer.Write(Cols);
			writer.Write(Extent);
			writer.Write(Density);

			for (ushort x = 0; x < Cols; x++)
			{
				for (ushort y = 0; y < Rows; y++)
				{
					PointCloudTile tile = m_tiles[x, y];
					writer.Write(tile.PointCount);
					writer.Write(tile.StorageSize);
					writer.Write(tile.QuantizedExtent);
				}
			}
		}

		#endregion

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
			// make sure we traverse the tiles in the correct storage order (so we don't have to seek)
			for (int tileX = 0; tileX < Cols; tileX++)
			{
				for (int tileY = 0; tileY < Rows; tileY++)
				{
					//if (m_tiles[tileX, tileY].IsValid)
						yield return m_tiles[tileX, tileY];
				}
			}
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
