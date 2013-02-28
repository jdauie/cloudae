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

		public GridIndexSegments GetGridIndex(PointCloudTileDensity density)
		{
			const int segmentSize = (int)ByteSizesSmall.MB_256;

			m_actualGrid = density.CreateTileCountsForInitialization(false);

			var regionSourcesBySegment = new List<SortedDictionary<int, int>>();
			var tilesPerSegment = new List<int>();
			long pointDataBytes = m_source.PointSizeBytes * m_source.Count;
			long pointDataRemainingBytes = pointDataBytes;

			int tilesChecked = 0;

			int chunksPerSegment = segmentSize / (m_maxPointCountPerChunk * m_source.PointSizeBytes);

			while (pointDataRemainingBytes > 0)
			{
				var cellList = new HashSet<int>();
				int currentSize = 0;
				int currentTiles = 0;
				// this repeated enumeration can be improved later
				foreach (var tile in PointCloudTileSet.GetTileOrdering(m_actualGrid.SizeY, m_actualGrid.SizeX).Skip(tilesChecked))
				{
					// unfortunately, I cannot check the actual counts, because they have not been measured
					// instead, I can count the estimated area, and undershoot

					var correspondingEstimatedCounts = m_estimatedCounts.GetCellsInScaledRange(tile.Col, tile.Row, m_actualGrid).ToList();

					var count = correspondingEstimatedCounts.Sum();
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
						if (cellList.Count + cellList2.Count > chunksPerSegment)
							break;

						foreach (var index in cellList2)
							cellList.Add(index);

						currentSize += tileSize;
						++currentTiles;
					}

					++tilesChecked;
				}

				// group by sequential regions
				int lastIndex = -2;
				var regions = new SortedDictionary<int, int>();
				int[] sortedCellList = cellList.ToArray();
				Array.Sort(sortedCellList);
				foreach (var index in sortedCellList)
				{
					if (regions.Count == 0 || index > lastIndex + regions[lastIndex])
					{
						lastIndex = index;
						regions.Add(index, 1);
					}
					else
					{
						++regions[lastIndex];
					}
				}

				regionSourcesBySegment.Add(regions);
				tilesPerSegment.Add(currentTiles);
				pointDataRemainingBytes -= currentSize;
			}

			int chunkRangeSumForAllSegments = regionSourcesBySegment.Sum(r => r.Sum(kvp => kvp.Value));
			Context.WriteLine("chunkRangeSumForAllSegments: {0}", chunkRangeSumForAllSegments);
			Context.WriteLine("  ratio: {0}", (double)chunkRangeSumForAllSegments * (int)ByteSizesSmall.MB_1 / pointDataBytes);

			var gridIndexSegments = new GridIndexSegments(regionSourcesBySegment, tilesPerSegment, m_maxPointCountPerChunk);
			return gridIndexSegments;
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
