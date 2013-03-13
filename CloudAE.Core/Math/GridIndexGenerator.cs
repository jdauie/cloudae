//using System;
//using System.Collections.Generic;
//using System.Linq;

//using Jacere.Core;
//using Jacere.Data.PointCloud;

//namespace CloudAE.Core
//{
//	public class GridIndexGenerator
//	{
//		private IPointCloudBinarySource m_source;
//		private Grid<GridIndexCell> m_estimatedIndex;
//		private Grid<int> m_estimatedCounts;
//		private Grid<int> m_actualGrid;
//		private int m_maxPointCountPerChunk;

//		public GridIndexGenerator()
//		{
//		}

//		public List<PointCloudBinarySourceEnumeratorSparseGridRegion> GetGridIndex(PointCloudTileDensity density)
//		{
//			const int segmentSize = (int)ByteSizesSmall.MB_512;

//			m_actualGrid = density.CreateTileCountsForInitialization(false);

//			var regionSourcesBySegment = new List<List<Range>>();
//			var tilesPerSegment = new List<GridRange>();
//			long pointDataBytes = m_source.PointSizeBytes * m_source.Count;

//			int pointsPerSegment = segmentSize / m_source.PointSizeBytes;
//			//int chunksPerSegment = segmentSize / (m_maxPointCountPerChunk * m_source.PointSizeBytes);

//			var tileOrder = PointCloudTileSet.GetTileOrdering(m_actualGrid.SizeY, m_actualGrid.SizeX).ToArray();
//			int tileOrderIndex = 0;
//			while (tileOrderIndex < tileOrder.Length)
//			{
//				var segmentTilesFromEstimation = new HashSet<int>();
//				var segmentChunks = new HashSet<int>();

//				var startTile = tileOrder[tileOrderIndex];
//				var startTileIndex = new GridCoord(m_actualGrid.Def, startTile.Row, startTile.Col);

//				int segmentPointCount = 0;

//				while (tileOrderIndex < tileOrder.Length)
//				{
//					var tile = tileOrder[tileOrderIndex];

//					// unfortunately, I cannot check the actual counts, because they have not been measured
//					// instead, I can count the estimated area, and undershoot

//					// get unique tiles/chunks
					
//					var uniqueEstimatedCoords = m_estimatedIndex
//						.GetCellCoordsInScaledRange(tile.Col, tile.Row, m_actualGrid)
//						.Where(c => !segmentTilesFromEstimation.Contains(c.Index))
//						.ToList();

//					var uniquePointCount = uniqueEstimatedCoords.Sum(c => m_estimatedCounts.Data[c.Row, c.Col]);

//					if (segmentPointCount + uniquePointCount > pointsPerSegment)
//						break;

//					var uniqueChunks = uniqueEstimatedCoords
//						.Select(c => m_estimatedIndex.Data[c.Row, c.Col])
//						.SelectMany(c => c.Chunks)
//						.ToHashSet(segmentChunks);

//					// this is safe, but it can create smaller segments than necessary
//					//if (segmentChunks.Count + uniqueChunks.Count > chunksPerSegment)
//					//	break;

//					// merge tiles/chunks
//					foreach(var coord in uniqueEstimatedCoords)
//						segmentTilesFromEstimation.Add(coord.Index);

//					foreach (var index in uniqueChunks)
//						segmentChunks.Add(index);

//					segmentPointCount += uniquePointCount;

//					++tileOrderIndex;
//				}

//				if (segmentChunks.Count > 0)
//				{
//					var endTile = tileOrder[tileOrderIndex - 1];
//					var endTileIndex = new GridCoord(m_actualGrid.Def, endTile.Row, endTile.Col);

//					// group by sequential regions
//					int[] sortedCellList = segmentChunks.ToArray();
//					Array.Sort(sortedCellList);
//					var regions = new List<Range>();
//					int sequenceStartIndex = 0;
//					while (sequenceStartIndex < sortedCellList.Length)
//					{
//						// find incremental sequence
//						int i = sequenceStartIndex;
//						++i;
//						while (i < sortedCellList.Length && (sortedCellList[i] == sortedCellList[i - 1] + 1))
//							++i;
//						regions.Add(new Range(sortedCellList[sequenceStartIndex], i - sequenceStartIndex));
//						sequenceStartIndex = i;
//					}

//					regionSourcesBySegment.Add(regions);
//					tilesPerSegment.Add(new GridRange(startTileIndex, endTileIndex));
//				}
//			}

//			int chunkRangeSumForAllSegments = regionSourcesBySegment.Sum(r => r.Sum(r2 => r2.Count));
//			Context.WriteLine("chunkRangeSumForAllSegments: {0}", chunkRangeSumForAllSegments);
//			Context.WriteLine("  ratio: {0}", (double)chunkRangeSumForAllSegments * (int)ByteSizesSmall.MB_1 / pointDataBytes);

//			return regionSourcesBySegment.Select((t, i) => new PointCloudBinarySourceEnumeratorSparseGridRegion(t, tilesPerSegment[i], m_maxPointCountPerChunk)).ToList();
//		}

//		public void Update(IPointCloudBinarySource source, Grid<GridIndexCell> estimatedIndex, Grid<int> estimatedCounts, int maxPointCountPerChunk)
//		{
//			m_source = source;
//			m_estimatedIndex = estimatedIndex;
//			m_estimatedCounts = estimatedCounts;
//			m_maxPointCountPerChunk = maxPointCountPerChunk;
//		}
//	}
//}
