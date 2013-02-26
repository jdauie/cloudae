using System;
using System.Collections.Generic;
using System.Linq;

using Jacere.Core;
using Jacere.Core.Geometry;
using Jacere.Data.PointCloud;

namespace CloudAE.Core
{
	// in order for this to make sense, the chunk index needs to map back to the input
	// it would be nice if I could get sector-aligned numbers, but that's too much for now
	public class GridIndexCell
	{
		private readonly HashSet<int> m_chunks;

		public IEnumerable<int> Chunks
		{
			get { return m_chunks; }
		}

		public bool HasChunks
		{
			get { return m_chunks.Count > 0; }
		}

		public GridIndexCell()
		{
			m_chunks = new HashSet<int>();
		}

		public GridIndexCell(GridIndexCell a, GridIndexCell b)
		{
			m_chunks = new HashSet<int>();

			if (a != null)
				foreach (var c in a.m_chunks)
					m_chunks.Add(c);

			if (b != null)
				foreach (var c in b.m_chunks)
					m_chunks.Add(c);
		}

		public void Add(int chunkIndex)
		{
			m_chunks.Add(chunkIndex);
		}

		public override string ToString()
		{
			// debugging only
			return string.Format("{0}", string.Join(",", m_chunks));
		}
	}

	public class GridIndexSegments : IEnumerable<PointCloudBinarySourceEnumeratorSparseRegion>
	{
		private readonly List<PointCloudBinarySourceEnumeratorSparseRegion> m_regionSourcesBySegment;
		private readonly int[] m_tilesPerSegment;

		public int GetSegmentTileRange(PointCloudBinarySourceEnumeratorSparseRegion region)
		{
			int index = m_regionSourcesBySegment.IndexOf(region);
			if (index == -1)
				throw new Exception("Region does not belong");

			return m_tilesPerSegment[index];
		}

		public GridIndexSegments(IEnumerable<SortedDictionary<int, int>> regionSourcesBySegment, List<int> tilesPerSegment, int maxPointCountPerChunk)
		{
			m_regionSourcesBySegment = new List<PointCloudBinarySourceEnumeratorSparseRegion>();
			foreach (var segment in regionSourcesBySegment)
				m_regionSourcesBySegment.Add(new PointCloudBinarySourceEnumeratorSparseRegion(segment, maxPointCountPerChunk));
			m_tilesPerSegment = tilesPerSegment.ToArray();

			if (m_regionSourcesBySegment.Count != m_tilesPerSegment.Length)
				throw new Exception("GridIndex Tile/Segment mismatch");
		}

		#region IEnumerable Members

		public IEnumerator<PointCloudBinarySourceEnumeratorSparseRegion> GetEnumerator()
		{
			return m_regionSourcesBySegment.GetEnumerator();
		}

		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		#endregion
	}

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

	public class GridCounter : IDisposable, IChunkProcess
	{
		private readonly IPointCloudBinarySource m_source;
		private readonly Grid<int> m_grid;
		private readonly bool m_quantized;

		private readonly Extent3D m_extent;

		private readonly double m_tilesOverRangeX;
		private readonly double m_tilesOverRangeY;

		private readonly GridIndexGenerator m_gridIndexGenerator;
		private readonly Grid<GridIndexCell> m_gridIndex;

		private int m_maxPointCount;

		public GridCounter(IPointCloudBinarySource source, Grid<int> grid)
			: this(source, grid, null)
		{
		}

		public GridCounter(IPointCloudBinarySource source, Grid<int> grid, GridIndexGenerator gridIndexGenerator)
		{
			m_source = source;
			m_grid = grid;
			m_extent = m_source.Extent;
			m_quantized = (m_source.Quantization != null);

			if (m_quantized)
			{
				var inputQuantization = (SQuantization3D)source.Quantization;
				var q = (SQuantizedExtent3D)inputQuantization.Convert(m_extent);
				m_extent = new Extent3D(q.MinX, q.MinY, q.MinZ, q.MaxX, q.MaxY, q.MaxZ);
			}

			m_tilesOverRangeX = m_grid.SizeX / m_extent.RangeX;
			m_tilesOverRangeY = m_grid.SizeY / m_extent.RangeY;

			if (gridIndexGenerator != null)
			{
				m_gridIndexGenerator = gridIndexGenerator;
				m_gridIndex = new Grid<GridIndexCell>(grid.SizeX, grid.SizeY, null, true);
			}
		}

		public unsafe IPointDataChunk Process(IPointDataChunk chunk)
		{
			if (chunk.PointCount > m_maxPointCount)
				m_maxPointCount = chunk.PointCount;

			if (m_quantized)
			{
				// get the tile indices for this chunk
				var tileIndices = new HashSet<PointCloudTileCoord>();
				PointCloudTileCoord lastIndex = PointCloudTileCoord.Empty;

				byte* pb = chunk.PointDataPtr;
				while (pb < chunk.PointDataEndPtr)
				{
					SQuantizedPoint3D* p = (SQuantizedPoint3D*)pb;

					//ushort tileX = (ushort)(((*p).X - m_extent.MinX) * m_tilesOverRangeX);
					//ushort tileY = (ushort)(((*p).Y - m_extent.MinY) * m_tilesOverRangeY);

					PointCloudTileCoord tileIndex = new PointCloudTileCoord(
						(ushort)(((*p).Y - m_extent.MinY) * m_tilesOverRangeY), 
						(ushort)(((*p).X - m_extent.MinX) * m_tilesOverRangeX)
					);

					//if (tileX < 0) tileX = 0; else if (tileX > m_grid.SizeX) tileX = m_grid.SizeX;
					//if (tileY < 0) tileY = 0; else if (tileY > m_grid.SizeY) tileY = m_grid.SizeY;

					++m_grid.Data[tileIndex.Row, tileIndex.Col];

					// indexing
					if(m_gridIndex != null)
					{
						if (tileIndex != lastIndex)
						{
							tileIndices.Add(tileIndex);
							lastIndex = tileIndex;
						}
					}

					pb += chunk.PointSizeBytes;
				}

				// update index cells
				if (m_gridIndex != null)
				{
					foreach (var tileIndex in tileIndices)
					{
						var indexCell = m_gridIndex.Data[tileIndex.Row, tileIndex.Col];
						if (indexCell == null)
						{
							indexCell = new GridIndexCell();
							m_gridIndex.Data[tileIndex.Row, tileIndex.Col] = indexCell;
						}
						indexCell.Add(chunk.Index);
					}
				}
			}
			else
			{
				byte* pb = chunk.PointDataPtr;
				while (pb < chunk.PointDataEndPtr)
				{
					Point3D* p = (Point3D*)pb;
					++m_grid.Data[
						(int)(((*p).Y - m_extent.MinY) * m_tilesOverRangeY),
						(int)(((*p).X - m_extent.MinX) * m_tilesOverRangeX)
					];

					pb += chunk.PointSizeBytes;
				}
			}

			return chunk;
		}

		public void Dispose()
		{
			m_grid.CorrectCountOverflow();

			if (m_gridIndex != null)
			{
				m_gridIndex.CorrectCountOverflow();
				m_gridIndexGenerator.Update(m_source, m_gridIndex, m_grid, m_maxPointCount);
			}
		}
	}
}
