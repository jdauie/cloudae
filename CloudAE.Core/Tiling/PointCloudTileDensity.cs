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

		private SQuantizedExtentGrid<int> m_tileCountsForInitialization;

		public PointCloudTileDensity(Grid<int> tileCounts, Extent3D extent)
		{
			var counts = tileCounts.Data.Cast<int>();

			Extent = extent;

			TileCount = tileCounts.CellCount;

			var nonZeroCounts = counts.Where(c => c > 0).ToArray();
			Array.Sort(nonZeroCounts);
			PointCount = nonZeroCounts.SumLong();

#warning update this when tiles are square
			var tileArea = Extent.Area / TileCount;

			ValidTileCount = nonZeroCounts.Length;

			MinTileCount = nonZeroCounts[0];
			MaxTileCount = nonZeroCounts[ValidTileCount - 1];
			MedianTileCount = nonZeroCounts[ValidTileCount / 2];
			MeanTileCount = (int)(PointCount / ValidTileCount);

			MinTileDensity = MinTileCount / tileArea;
			MaxTileDensity = MaxTileCount / tileArea;
			MedianTileDensity = MedianTileCount / tileArea;
			MeanTileDensity = MeanTileCount / tileArea;
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

		public SQuantizedExtentGrid<int> GetTileCountsForInitialization()
		{
			// todo: do I need this?
			//m_tileCountsForInitialization.Reset();

			return m_tileCountsForInitialization;
		}

		public GridDefinition CreateTileCountsForInitialization(SQuantizedExtent3D extent, SQuantization3D quantization)
		{
			if (m_tileCountsForInitialization == null)
			{
				// median works better usually, but max is safer for substantially varying density
				// (like terrestrial, although that requires a more thorough redesign)
				//double tileArea = PROPERTY_DESIRED_TILE_COUNT.Value / density.MaxTileDensity;
				var tileArea = PointCloudTileManager.PROPERTY_DESIRED_TILE_COUNT.Value / MedianTileDensity;
				var tileSize = Math.Sqrt(tileArea);

				Context.WriteLine("TileSide: {0}", tileSize);

				m_tileCountsForInitialization = extent.CreateGridFromCellSize<int>(tileSize, quantization, true);
			}
			return m_tileCountsForInitialization.Def;
		}

		public override string ToString()
		{
			return String.Format("{0:0.####}", MedianTileDensity);
		}
	}
}
