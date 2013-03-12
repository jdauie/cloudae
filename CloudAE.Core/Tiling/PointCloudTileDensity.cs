using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;

using Jacere.Core;
using Jacere.Core.Geometry;

namespace CloudAE.Core
{
	public class PointCloudTileDensity : ISerializeBinary
	{
		public readonly long PointCount;
		public readonly int TileCount;
		public readonly int ValidTileCount;

		public readonly int MinTileCount;
		public readonly int MaxTileCount;
		public readonly int MedianTileCount;
		public readonly int MeanTileCount;

		public readonly double MinTileDensity;
		public readonly double MaxTileDensity;
		public readonly double MedianTileDensity;
		public readonly double MeanTileDensity;

		public readonly Extent3D Extent;

		private Grid<int> m_tileCountsForInitialization;

		public PointCloudTileDensity(Grid<int> tileCounts, Extent3D extent)
			: this(tileCounts.CellCount, tileCounts.Data.Cast<int>(), extent)
		{
		}

		public PointCloudTileDensity(int tileCount, IEnumerable<int> counts, Extent3D extent)
		{
			Extent = extent;

			TileCount = tileCount;

			int[] nonZeroCounts = counts.Where(c => c > 0).ToArray();
			Array.Sort(nonZeroCounts);
			PointCount = nonZeroCounts.Sum();

			double tileArea = Extent.Area / TileCount;

			ValidTileCount = nonZeroCounts.Length;

#warning This is just so I can create a fake/temporary one of these
			if (nonZeroCounts.Length > 0)
			{
				MinTileCount = nonZeroCounts[0];
				MaxTileCount = nonZeroCounts[ValidTileCount - 1];
				MedianTileCount = nonZeroCounts[ValidTileCount / 2];
				MeanTileCount = (int)(PointCount / ValidTileCount);

				MinTileDensity = MinTileCount / tileArea;
				MaxTileDensity = MaxTileCount / tileArea;
				MedianTileDensity = MedianTileCount / tileArea;
				MeanTileDensity = MeanTileCount / tileArea;
			}
		}

		public PointCloudTileDensity(BinaryReader reader)
		{
			PointCount        = reader.ReadInt64();
			TileCount         = reader.ReadInt32();
			ValidTileCount    = reader.ReadInt32();

			MinTileCount      = reader.ReadInt32();
			MaxTileCount      = reader.ReadInt32();
			MedianTileCount   = reader.ReadInt32();
			MeanTileCount     = reader.ReadInt32();

			MinTileDensity    = reader.ReadDouble();
			MaxTileDensity    = reader.ReadDouble();
			MedianTileDensity = reader.ReadDouble();
			MeanTileDensity   = reader.ReadDouble();
		}

		public void Serialize(BinaryWriter writer)
		{
			writer.Write(PointCount);
			writer.Write(TileCount);
			writer.Write(ValidTileCount);

			writer.Write(MinTileCount);
			writer.Write(MaxTileCount);
			writer.Write(MedianTileCount);
			writer.Write(MeanTileCount);

			writer.Write(MinTileDensity);
			writer.Write(MaxTileDensity);
			writer.Write(MedianTileDensity);
			writer.Write(MeanTileDensity);
		}

		public Grid<int> CreateTileCountsForInitialization(bool clear)
		{
			if (m_tileCountsForInitialization == null)
			{
				Extent3D extent = Extent;

				// median works better usually, but max is safer for substantially varying density
				// (like terrestrial, although that requires a more thorough redesign)
				//double tileArea = PROPERTY_DESIRED_TILE_COUNT.Value / density.MaxTileDensity;
				double tileArea = PointCloudTileManager.PROPERTY_DESIRED_TILE_COUNT.Value / MedianTileDensity;
				double tileSide = Math.Sqrt(tileArea);

#warning this results in non-square tiles

				ushort tilesX = (ushort)Math.Ceiling(extent.RangeX / tileSide);
				ushort tilesY = (ushort)Math.Ceiling(extent.RangeY / tileSide);

				m_tileCountsForInitialization = Grid<int>.CreateBuffered(tilesX, tilesY, extent);
			}
			else if (clear)
			{
				m_tileCountsForInitialization.Reset();
			}

			return m_tileCountsForInitialization;
		}

		public override string ToString()
		{
			return String.Format("{0:0.####}", MedianTileDensity);
		}
	}
}
