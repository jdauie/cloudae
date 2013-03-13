using System;
using System.Collections.Generic;
using System.Linq;

using Jacere.Core;
using Jacere.Core.Geometry;
using Jacere.Data.PointCloud;

namespace CloudAE.Core
{
	public class GridCounter : IChunkProcess, IFinalizeProcess
	{
		private readonly IPointCloudBinarySource m_source;
		private readonly Grid<int> m_grid;

		private readonly SQuantizedExtent3D m_extent;

		private readonly Dictionary<int, int[]> m_chunkTiles;

		private int m_maxPointCountPerChunk;

		public GridCounter(IPointCloudBinarySource source, Grid<int> grid)
		{
			m_source = source;
			m_grid = grid;
			m_extent = source.Quantization.Convert(m_source.Extent);

			m_chunkTiles = new Dictionary<int, int[]>();
		}

		public unsafe IPointDataChunk Process(IPointDataChunk chunk)
		{
			if (chunk.PointCount > m_maxPointCountPerChunk)
				m_maxPointCountPerChunk = chunk.PointCount;

			double minX = m_extent.MinX;
			double minY = m_extent.MinY;
			double tilesOverRangeX = (double)m_grid.SizeX / m_extent.RangeX;
			double tilesOverRangeY = (double)m_grid.SizeY / m_extent.RangeY;

			// get the tile indices for this chunk
			var tileIndices = new HashSet<int>();
			var lastIndex = -1;

			var pb = chunk.PointDataPtr;
			while (pb < chunk.PointDataEndPtr)
			{
				var p = (SQuantizedPoint3D*)pb;

				var y = (ushort)(((*p).Y - minY) * tilesOverRangeY);
				var x = (ushort)(((*p).X - minX) * tilesOverRangeX);

				++m_grid.Data[y, x];

				// indexing
				int tileIndex = PointCloudTileCoord.GetIndex(y, x);
				if (tileIndex != lastIndex)
				{
					tileIndices.Add(tileIndex);
					lastIndex = tileIndex;
				}

				pb += chunk.PointSizeBytes;
			}

			m_chunkTiles.Add(chunk.Index, tileIndices.ToArray());

			return chunk;
		}

		public void FinalizeProcess()
		{
			m_grid.CorrectCountOverflow();
		}

		public List<PointCloudBinarySourceEnumeratorSparseGridRegion> GetGridIndex(PointCloudTileDensity density, int maxSegmentLength)
		{
			int maxSegmentPointCount = (maxSegmentLength / m_source.PointSizeBytes);

			// update index cells
			var indexGrid = m_grid.Copy<GridIndexCell>();
			foreach (var kvp in m_chunkTiles)
			{
				foreach (var tileIndex in kvp.Value)
				{
					var coord = new PointCloudTileCoord(tileIndex);
					var indexCell = indexGrid.Data[coord.Row, coord.Col];
					if (indexCell == null)
					{
						indexCell = new GridIndexCell();
						indexGrid.Data[coord.Row, coord.Col] = indexCell;
					}
					indexCell.Add(kvp.Key);
				}
			}
			indexGrid.CorrectCountOverflow();

			var actualGrid = density.CreateTileCountsForInitialization(false);

			var regionSourcesBySegment = new List<List<Range>>();
			var tilesPerSegment = new List<GridRange>();
			long pointDataBytes = m_source.PointSizeBytes * m_source.Count;

			var tileOrder = PointCloudTileSet.GetTileOrdering(actualGrid.SizeY, actualGrid.SizeX).ToArray();
			int tileOrderIndex = 0;
			while (tileOrderIndex < tileOrder.Length)
			{
				var segmentTilesFromEstimation = new HashSet<int>();
				var segmentChunks = new HashSet<int>();

				var startTile = tileOrder[tileOrderIndex];
				var startTileIndex = new GridCoord(actualGrid.Def, startTile.Row, startTile.Col);

				int segmentPointCount = 0;

				while (tileOrderIndex < tileOrder.Length)
				{
					var tile = tileOrder[tileOrderIndex];

					// unfortunately, I cannot check the actual counts, because they have not been measured
					// instead, I can count the estimated area, and undershoot

					// get unique tiles/chunks

					var uniqueEstimatedCoords = indexGrid
						.GetCellCoordsInScaledRange(tile.Col, tile.Row, actualGrid)
						.Where(c => !segmentTilesFromEstimation.Contains(c.Index))
						.ToList();

					var uniquePointCount = uniqueEstimatedCoords.Sum(c => m_grid.Data[c.Row, c.Col]);

					if (segmentPointCount + uniquePointCount > maxSegmentPointCount)
						break;

					var uniqueChunks = uniqueEstimatedCoords
						.Select(c => indexGrid.Data[c.Row, c.Col])
						.SelectMany(c => c.Chunks)
						.ToHashSet(segmentChunks);

					// this is safe, but it can create smaller segments than necessary
					//if (segmentChunks.Count + uniqueChunks.Count > chunksPerSegment)
					//	break;

					// merge tiles/chunks
					foreach (var coord in uniqueEstimatedCoords)
						segmentTilesFromEstimation.Add(coord.Index);

					foreach (var index in uniqueChunks)
						segmentChunks.Add(index);

					segmentPointCount += uniquePointCount;

					++tileOrderIndex;
				}

				if (segmentChunks.Count > 0)
				{
					var endTile = tileOrder[tileOrderIndex - 1];
					var endTileIndex = new GridCoord(actualGrid.Def, endTile.Row, endTile.Col);

					// group by sequential regions
					int[] sortedCellList = segmentChunks.ToArray();
					Array.Sort(sortedCellList);
					var regions = new List<Range>();
					int sequenceStartIndex = 0;
					while (sequenceStartIndex < sortedCellList.Length)
					{
						// find incremental sequence
						int i = sequenceStartIndex;
						++i;
						while (i < sortedCellList.Length && (sortedCellList[i] == sortedCellList[i - 1] + 1))
							++i;
						regions.Add(new Range(sortedCellList[sequenceStartIndex], i - sequenceStartIndex));
						sequenceStartIndex = i;
					}

					regionSourcesBySegment.Add(regions);
					tilesPerSegment.Add(new GridRange(startTileIndex, endTileIndex));
				}
			}

			int chunkRangeSumForAllSegments = regionSourcesBySegment.Sum(r => r.Sum(r2 => r2.Count));
			Context.WriteLine("chunkRangeSumForAllSegments: {0}", chunkRangeSumForAllSegments);
			Context.WriteLine("  ratio: {0}", (double)chunkRangeSumForAllSegments * (int)ByteSizesSmall.MB_1 / pointDataBytes);

			return regionSourcesBySegment.Select((t, i) => new PointCloudBinarySourceEnumeratorSparseGridRegion(t, tilesPerSegment[i], m_maxPointCountPerChunk)).ToList();
		}
	}
}
