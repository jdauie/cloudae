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
		private readonly SQuantizedExtentGrid<int> m_grid;

		private readonly SQuantizedExtent3D m_quantizedExtent;

		private readonly List<int[]> m_chunkTiles;

		private int m_maxPointCountPerChunk;

		public GridCounter(IPointCloudBinarySource source, SQuantizedExtentGrid<int> grid)
		{
			m_source = source;
			m_grid = grid;
			m_quantizedExtent = source.QuantizedExtent;

			m_chunkTiles = new List<int[]>();
		}

		public unsafe IPointDataChunk Process(IPointDataChunk chunk)
		{
			if (chunk.PointCount > m_maxPointCountPerChunk)
				m_maxPointCountPerChunk = chunk.PointCount;

			// get the tile indices for this chunk
			var tileIndices = new HashSet<int>();
			var lastIndex = -1;

			// JUST TESTING TO SEE IF I LIKE THIS WAY BETTER (slightly slower?)
			//var iterator = new SQuantizedPoint3DIterator(chunk);
			//while (iterator.IsValid)
			//{
			//    var p = iterator.Next();
			//    var y = (ushort)(((*p).Y - minY) * tilesOverRangeY);
			//    var x = (ushort)(((*p).X - minX) * tilesOverRangeX);

			//    ++m_grid.Data[y, x];

			//    // indexing
			//    int tileIndex = PointCloudTileCoord.GetIndex(y, x);
			//    if (tileIndex != lastIndex)
			//    {
			//        tileIndices.Add(tileIndex);
			//        lastIndex = tileIndex;
			//    }
			//}

			// JUST TESTING TO SEE IF I LIKE THIS WAY BETTER (slightly slower?)
			foreach (var pp in chunk.GetSQuantizedPoint3DEnumerator())
			{
				//var p = (LASPointFormat1*)pp.GetPointer();
				var p = pp.GetPointer();
				var y = (((*p).Y - m_quantizedExtent.MinY) / m_grid.CellSizeY);
				var x = (((*p).X - m_quantizedExtent.MinX) / m_grid.CellSizeX);

				++m_grid.Data[y, x];

				// indexing
				int tileIndex = PointCloudTileCoord.GetIndex(y, x);
				if (tileIndex != lastIndex)
				{
					tileIndices.Add(tileIndex);
					lastIndex = tileIndex;
				}
			}

			//var pb = chunk.PointDataPtr;
			//while (pb < chunk.PointDataEndPtr)
			//{
			//    var p = (SQuantizedPoint3D*)pb;

			//    var y = (ushort)(((*p).Y - minY) * tilesOverRangeY);
			//    var x = (ushort)(((*p).X - minX) * tilesOverRangeX);

			//    ++m_grid.Data[y, x];

			//    // indexing
			//    int tileIndex = PointCloudTileCoord.GetIndex(y, x);
			//    if (tileIndex != lastIndex)
			//    {
			//        tileIndices.Add(tileIndex);
			//        lastIndex = tileIndex;
			//    }

			//    pb += chunk.PointSizeBytes;
			//}

			m_chunkTiles.Add(tileIndices.ToArray());

			return chunk;
		}

		public void FinalizeProcess()
		{
			m_grid.CorrectCountOverflow();
		}

		public List<PointCloudBinarySourceEnumeratorSparseGridRegion> GetGridIndex(PointCloudTileDensity density, int maxSegmentLength)
		{
			var maxSegmentPointCount = (maxSegmentLength / m_source.PointSizeBytes);

			// update index cells
			var indexGrid = (SQuantizedExtentGrid<GridIndexCell>)m_grid.Copy<GridIndexCell>();

			for (var i = 0; i < m_chunkTiles.Count; i++)
			{
				foreach (var tileIndex in m_chunkTiles[i])
				{
					var coord = new PointCloudTileCoord(tileIndex);
					var indexCell = indexGrid.Data[coord.Row, coord.Col];
					if (indexCell == null)
					{
						indexCell = new GridIndexCell();
						indexGrid.Data[coord.Row, coord.Col] = indexCell;
					}
					indexCell.Add(i);
				}
			}
			indexGrid.CorrectCountOverflow();

			var actualGrid = density.CreateTileCountsForInitialization(m_source);

			var regionSourcesBySegment = new List<List<Range>>();
			var tilesPerSegment = new List<GridRange>();
			var pointDataBytes = m_source.PointSizeBytes * m_source.Count;

			var tileOrder = PointCloudTileSet.GetTileOrdering(actualGrid.SizeY, actualGrid.SizeX).ToArray();
			var tileOrderIndex = 0;
			while (tileOrderIndex < tileOrder.Length)
			{
				var segmentTilesFromEstimation = new HashSet<int>();
				var segmentChunks = new HashSet<int>();

				var startTile = tileOrder[tileOrderIndex];
				var startTileIndex = new GridCoord(actualGrid.Def, startTile.Row, startTile.Col);

				var segmentProbableValidTileCount = 0;

				var segmentPointCount = 0;

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

					// count probable valid tiles for low-res estimation
					if (uniqueEstimatedCoords.Count > 0)
					{
						var uniquePointCount = uniqueEstimatedCoords.Sum(c => m_grid.Data[c.Row, c.Col]);

						if (segmentPointCount + uniquePointCount > maxSegmentPointCount)
							break;

						var uniqueChunks = uniqueEstimatedCoords
							.Select(c => indexGrid.Data[c.Row, c.Col])
							.SelectMany(c => c.Chunks)
							.ToHashSet(segmentChunks);

						// no longer limiting the chunks per segment to fit buffer.
						// this means that the full chunk data will almost certainly 
						// *not* fit in the segment buffer, but the filtered data will.

						// merge tiles/chunks
						foreach (var coord in uniqueEstimatedCoords)
							segmentTilesFromEstimation.Add(coord.Index);

						foreach (var index in uniqueChunks)
							segmentChunks.Add(index);

						segmentPointCount += uniquePointCount;

						++segmentProbableValidTileCount;
					}

					++tileOrderIndex;
				}

				if (segmentChunks.Count > 0)
				{
					var endTile = tileOrder[tileOrderIndex - 1];
					var endTileIndex = new GridCoord(actualGrid.Def, endTile.Row, endTile.Col);

					// group by sequential regions
					var sortedCellList = segmentChunks.ToArray();
					Array.Sort(sortedCellList);
					var regions = new List<Range>();
					var sequenceStartIndex = 0;
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
					tilesPerSegment.Add(new GridRange(startTileIndex, endTileIndex, segmentProbableValidTileCount));
				}
			}

			var chunkRangeSumForAllSegments = regionSourcesBySegment.Sum(r => r.Sum(r2 => r2.Count));
			Context.WriteLine("chunkRangeSumForAllSegments: {0}", chunkRangeSumForAllSegments);
			Context.WriteLine("  ratio: {0}", (double)chunkRangeSumForAllSegments * BufferManager.BUFFER_SIZE_BYTES / pointDataBytes);

			return regionSourcesBySegment.Select((t, i) => new PointCloudBinarySourceEnumeratorSparseGridRegion(t, tilesPerSegment[i], m_maxPointCountPerChunk)).ToList();
		}
	}
}
