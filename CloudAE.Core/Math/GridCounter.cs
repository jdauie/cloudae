﻿using System;
using System.Collections.Generic;
using System.Linq;
using CloudAE.Core.Geometry;

namespace CloudAE.Core
{
	// in order for this to make sense, the chunk index needs to map back to the input
	// it would be nice if I could get sector-aligned numbers, but that's too much for now
	public class GridIndexCell
	{
		private readonly SortedSet<int> m_chunks;

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
			m_chunks = new SortedSet<int>();
		}

		public GridIndexCell(GridIndexCell a, GridIndexCell b)
		{
			m_chunks = new SortedSet<int>();

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

	public class PointCloudBinarySourceEnumeratorRegion
	{
		public readonly int ChunkStart;
		public readonly int ChunkCount;

		public PointCloudBinarySourceEnumeratorRegion(int chunkStart, int chunkCount)
		{
			ChunkStart = chunkStart;
			ChunkCount = chunkCount;
		}

		public override string ToString()
		{
			return string.Format("[{0}-{1}] ({2})", ChunkStart, ChunkStart + ChunkCount, ChunkCount);
		}
	}

	public class PointCloudBinarySourceEnumeratorSparseRegion : IEnumerable<PointCloudBinarySourceEnumeratorRegion>
	{
		private readonly List<PointCloudBinarySourceEnumeratorRegion> m_regions;
		private readonly int m_chunkCount;
		private readonly int m_pointsPerChunk;

		public int PointsPerChunk
		{
			get { return m_pointsPerChunk; }
		}

		public PointCloudBinarySourceEnumeratorSparseRegion(IEnumerable<KeyValuePair<int, int>> regions, int maxPointCountPerChunk)
		{
			m_pointsPerChunk = maxPointCountPerChunk;
			m_regions = new List<PointCloudBinarySourceEnumeratorRegion>();
			foreach (var region in regions)
			{
				var r = new PointCloudBinarySourceEnumeratorRegion(region.Key, region.Value);
				m_chunkCount += r.ChunkCount;
				m_regions.Add(r);
			}
		}

		public override string ToString()
		{
			return string.Format("{0} ({1})", m_regions.Count, m_chunkCount);
		}

		#region IEnumerable Members

		public IEnumerator<PointCloudBinarySourceEnumeratorRegion> GetEnumerator()
		{
			return m_regions.GetEnumerator();
		}

		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		#endregion
	}

	public class GridIndexSegments : IEnumerable<PointCloudBinarySourceEnumeratorSparseRegion>
	{
		private readonly List<PointCloudBinarySourceEnumeratorSparseRegion> m_regionSourcesBySegment;

		public GridIndexSegments(IEnumerable<SortedDictionary<int, int>> regionSourcesBySegment, int maxPointCountPerChunk)
		{
			m_regionSourcesBySegment = new List<PointCloudBinarySourceEnumeratorSparseRegion>();
			foreach (var segment in regionSourcesBySegment)
				m_regionSourcesBySegment.Add(new PointCloudBinarySourceEnumeratorSparseRegion(segment, maxPointCountPerChunk));
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
		private GridIndexSegments m_gridIndexSegments;

		public GridIndexGenerator()
		{
		}

		public GridIndexSegments GetGridIndex()
		{
			return m_gridIndexSegments;
		}

		public void Generate(IPointCloudBinarySource source, Grid<GridIndexCell> estimatedIndex, Grid<int> estimatedCounts, Grid<int> actualGrid, int maxPointCountPerChunk)
		{
			const int segmentSize = (int)ByteSizesSmall.MB_256;

			var regionSourcesBySegment = new List<SortedDictionary<int, int>>();
			long pointDataBytes = source.PointSizeBytes * source.Count;
			long pointDataRemainingBytes = pointDataBytes;

			int tilesChecked = 0;

			while (pointDataRemainingBytes > 0)
			{
				var cellList = new SortedSet<int>();
				int currentSize = 0;
				foreach (var tile in PointCloudTileSet.GetTileOrdering(actualGrid.SizeY, actualGrid.SizeX).Skip(tilesChecked))
				{
					// unfortunately, I cannot check the actual counts, because they have not been measured
					// instead, I can count the estimated area, and undershoot

					// get the x,y coords of the corresponding tiles
					// get the associated counts

					//var count = actualGrid.Data[tile.Col, tile.Row];
					//if (count > 0)
					//{
					//    int tileSize = (count * source.PointSizeBytes);
					//    if (currentSize + tileSize > segmentSize)
					//        break;

					// get the matching index cell(s)
					// NOT QUITE WHAT I WANT -- THIS SHOULD BE COORDS SO I CAN LOOK UP COUNTS
					// (unless I merge counts with index, which I hesitate to do until I am certain of this mechanism)
					var correspondingIndexCells = estimatedIndex.GetCellsInScaledRange(tile.Col, tile.Row, actualGrid).Where(indexCell => indexCell != null).ToList();

					//currentSize += tileSize;

					++tilesChecked;
				}

				//var sortedChunkIndices = cellList.OrderBy(i => i).ToArray();

				// group by sequential regions
				int lastIndex = -2;
				var regions = new SortedDictionary<int, int>();
				foreach (var index in cellList)
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
				pointDataRemainingBytes -= currentSize;
			}

			int chunkRangeSumForAllSegments = regionSourcesBySegment.Sum(r => r.Sum(kvp => kvp.Value));
			Context.WriteLine("chunkRangeSumForAllSegments: {0}", chunkRangeSumForAllSegments);
			Context.WriteLine("  ratio: {0}", (double)chunkRangeSumForAllSegments * (int)ByteSizesSmall.MB_1 / pointDataBytes);

			m_gridIndexSegments = new GridIndexSegments(regionSourcesBySegment, maxPointCountPerChunk);
		}
	}

	public class GridCounter : IDisposable
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

		public unsafe void Process(IPointDataChunk chunk)
		{
			if (chunk.PointCount > m_maxPointCount)
				m_maxPointCount = chunk.PointCount;

			if (m_quantized)
			{
				byte* pb = chunk.PointDataPtr;
				while (pb < chunk.PointDataEndPtr)
				{
					SQuantizedPoint3D* p = (SQuantizedPoint3D*)pb;

					int tileX = (int)(((*p).X - m_extent.MinX) * m_tilesOverRangeX);
					int tileY = (int)(((*p).Y - m_extent.MinY) * m_tilesOverRangeY);

					//if (tileX < 0) tileX = 0; else if (tileX > m_grid.SizeX) tileX = m_grid.SizeX;
					//if (tileY < 0) tileY = 0; else if (tileY > m_grid.SizeY) tileY = m_grid.SizeY;

					++m_grid.Data[tileX, tileY];

					// indexing
					if(m_gridIndex != null)
					{
						var indexCell = m_gridIndex.Data[tileX, tileY];
						if (indexCell == null)
						{
							indexCell = new GridIndexCell();
							m_gridIndex.Data[tileX, tileY] = indexCell;
						}
						indexCell.Add(chunk.Index);
					}

					pb += chunk.PointSizeBytes;
				}
			}
			else
			{
				byte* pb = chunk.PointDataPtr;
				while (pb < chunk.PointDataEndPtr)
				{
					Point3D* p = (Point3D*)pb;
					++m_grid.Data[
						(int)(((*p).X - m_extent.MinX) * m_tilesOverRangeX),
						(int)(((*p).Y - m_extent.MinY) * m_tilesOverRangeY)
					];

					pb += chunk.PointSizeBytes;
				}
			}
		}

		public void Dispose()
		{
			m_grid.CorrectCountOverflow();

			if (m_gridIndex != null)
			{
				m_gridIndex.CorrectCountOverflow();
				m_gridIndexGenerator.Generate(m_source, m_gridIndex, m_grid, null, m_maxPointCount);
			}
		}
	}
}
