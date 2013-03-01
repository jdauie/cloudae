using System;
using System.Collections.Generic;
using System.Linq;

using Jacere.Core;
using Jacere.Data.PointCloud;

namespace CloudAE.Core
{
	public class GridIndexGenerator
	{
		private IPointCloudBinarySource m_source;
		private Grid<GridIndexCell> m_estimatedIndex;
		private Grid<int> m_estimatedCounts;
		private Grid<int> m_actualGrid;
		private int m_maxPointCountPerChunk;

		public GridIndexGenerator()
		{
		}

		public List<PointCloudBinarySourceEnumeratorSparseGridRegion> GetGridIndex(PointCloudTileDensity density)
		{
			const int segmentSize = (int)ByteSizesSmall.MB_256;

			m_actualGrid = density.CreateTileCountsForInitialization(false);

			var regionSourcesBySegment = new List<List<Range>>();
			var tilesPerSegment = new List<GridRange>();
			long pointDataBytes = m_source.PointSizeBytes * m_source.Count;
			long pointDataRemainingBytes = pointDataBytes;

			int chunksPerSegment = segmentSize / (m_maxPointCountPerChunk * m_source.PointSizeBytes);

			var tileOrder = PointCloudTileSet.GetTileOrdering(m_actualGrid.SizeY, m_actualGrid.SizeX).ToArray();
			int tileOrderIndex = 0;
			while (pointDataRemainingBytes > 0)
			{
				var cellList = new HashSet<int>();
				int currentSize = 0;

				var startTile = tileOrder[tileOrderIndex];
				var startTileIndex = new GridCoord(m_actualGrid.Def, startTile.Row, startTile.Col);

				while (tileOrderIndex < tileOrder.Length)
				{
					var tile = tileOrder[tileOrderIndex];

					// unfortunately, I cannot check the actual counts, because they have not been measured
					// instead, I can count the estimated area, and undershoot

					var count = m_estimatedCounts.GetCellsInScaledRange(tile.Col, tile.Row, m_actualGrid).Sum();
					if (count > 0)
					{
						int tileSize = (count * m_source.PointSizeBytes);
						if (currentSize + tileSize > segmentSize)
							break;

						// get unique chunks
						var cellList2 = new HashSet<int>();
						var correspondingEstimatedIndex = m_estimatedIndex.GetCellsInScaledRange(tile.Col, tile.Row, m_actualGrid).ToList();
						foreach (var estimatedIndex in correspondingEstimatedIndex)
							foreach (var index in estimatedIndex.Chunks)
								if (!cellList.Contains(index))
									cellList2.Add(index);

						// undershoot again by counting the data read, not just the valid points
						// I really only need to do this for testing -- it slows things down a bit.
						// Once I am sure that filtering is working properly, this won't matter.
#warning remove this once I'm sure
						//if (cellList.Count + cellList2.Count > chunksPerSegment)
						//    break;

						foreach (var index in cellList2)
							cellList.Add(index);

						currentSize += tileSize;
					}

					++tileOrderIndex;
				}


				var endTile = tileOrder[tileOrderIndex - 1];
				var endTileIndex = new GridCoord(m_actualGrid.Def, endTile.Row, endTile.Col);

				// group by sequential regions
				int[] sortedCellList = cellList.ToArray();
				Array.Sort(sortedCellList);
				var regions = new List<Range>();
				int sequenceStartIndex = 0;
				while (sequenceStartIndex < sortedCellList.Length)
				{
					// find incremental sequence
					int i = sequenceStartIndex;
					while (i < sortedCellList.Length && (i == 0 || sortedCellList[i] == sortedCellList[i - 1] + 1))
						++i;
					regions.Add(new Range(sortedCellList[sequenceStartIndex], i - sequenceStartIndex));
					sequenceStartIndex = i;
				}

				regionSourcesBySegment.Add(regions);
				tilesPerSegment.Add(new GridRange(startTileIndex, endTileIndex));
				pointDataRemainingBytes -= currentSize;
			}

			int chunkRangeSumForAllSegments = regionSourcesBySegment.Sum(r => r.Sum(r2 => r2.Count));
			Context.WriteLine("chunkRangeSumForAllSegments: {0}", chunkRangeSumForAllSegments);
			Context.WriteLine("  ratio: {0}", (double)chunkRangeSumForAllSegments * (int)ByteSizesSmall.MB_1 / pointDataBytes);

			return regionSourcesBySegment.Select((t, i) => new PointCloudBinarySourceEnumeratorSparseGridRegion(t, tilesPerSegment[i], m_maxPointCountPerChunk)).ToList();
		}

		public void Update(IPointCloudBinarySource source, Grid<GridIndexCell> estimatedIndex, Grid<int> estimatedCounts, int maxPointCountPerChunk)
		{
			m_source = source;
			m_estimatedIndex = estimatedIndex;
			m_estimatedCounts = estimatedCounts;
			m_maxPointCountPerChunk = maxPointCountPerChunk;
		}
	}
}
