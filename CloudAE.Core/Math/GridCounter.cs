using System;
using System.Collections.Generic;
using System.Linq;
using CloudAE.Core.Geometry;

namespace CloudAE.Core
{
	// in order for this to make sense, the chunk index needs to map back to the input
	// it would be nice if I could get sector-aligned numbers, but that's too much for now
	public class GridIndexCell
	{
		private readonly Dictionary<int, int> m_chunkCounts;

		public IEnumerable<int> Chunks
		{
			get { return m_chunkCounts.Keys; }
		}

		public int Count
		{
			get { return m_chunkCounts.Values.Sum(); }
		}

		public GridIndexCell()
		{
			m_chunkCounts = new Dictionary<int, int>();
		}

		public GridIndexCell(GridIndexCell a, GridIndexCell b)
		{
			m_chunkCounts = new Dictionary<int, int>();

			if (a != null)
			{
				foreach (var kvp in a.m_chunkCounts)
					m_chunkCounts.Add(kvp.Key, kvp.Value);
			}

			if (b != null)
			{
				foreach (var kvp in b.m_chunkCounts)
				{
					if (!m_chunkCounts.ContainsKey(kvp.Key))
						m_chunkCounts.Add(kvp.Key, kvp.Value);
					else
						m_chunkCounts[kvp.Key] += kvp.Value;
				}
			}
		}

		public void Add(int chunkIndex)
		{
			int count = 0;
			if (!m_chunkCounts.TryGetValue(chunkIndex, out count))
				m_chunkCounts.Add(chunkIndex, 1);
			else
				++m_chunkCounts[chunkIndex];
		}

		public override string ToString()
		{
			// debugging only
			return string.Format("{0}", string.Join(",", m_chunkCounts.Keys));
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

		private readonly Grid<GridIndexCell> m_gridIndex;

		public GridCounter(IPointCloudBinarySource source, Grid<int> grid)
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

			m_tilesOverRangeX = (double)m_grid.SizeX / m_extent.RangeX;
			m_tilesOverRangeY = (double)m_grid.SizeY / m_extent.RangeY;

			m_gridIndex = new Grid<GridIndexCell>(grid.SizeX, grid.SizeY, null, true);
		}

		public unsafe void Process(IPointDataChunk chunk)
		{
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



					var indexCell = m_gridIndex.Data[tileX, tileY];
					if (indexCell == null)
					{
						indexCell = new GridIndexCell();
						m_gridIndex.Data[tileX, tileY] = indexCell;
					}
					indexCell.Add(chunk.Index);



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
			m_gridIndex.CorrectCountOverflow();

			// for testing purposes, figure out how much I have to read for each tile, 
			// relative to how much the tile contains
			// Also, it would be good to calculate that in aggregate
			// (e.g. how much to I have to read to get the first 256 MB of tiles)
			// This latter operation is a bit out of place since I don't know the tile order at this point

			var regionSourcesBySegment = new List<SortedDictionary<int, int>>();
			long pointDataBytes = m_source.PointSizeBytes * m_source.Count;
			long pointDataRemainingBytes = pointDataBytes;

			int tilesChecked = 0;

			while (pointDataRemainingBytes > 0)
			{
				var cellList = new Dictionary<int, int>();
				int segmentSize = (int)ByteSizesSmall.MB_256;
				int currentSize = 0;
				foreach (var tile in PointCloudTileSet.GetTileOrdering(m_grid.SizeY, m_grid.SizeX).Skip(tilesChecked))
				{
					var indexCell = m_gridIndex.Data[tile.Col, tile.Row];
					if (indexCell != null)
					{
						int count = indexCell.Count;
						if (count > 0)
						{
							int tileSize = (count * m_source.PointSizeBytes);
							if (currentSize + tileSize > segmentSize)
								break;

							foreach (var chunkIndex in indexCell.Chunks)
							{
								if (cellList.ContainsKey(chunkIndex))
									cellList[chunkIndex]++;
								else
									cellList.Add(chunkIndex, 1);
							}

							currentSize += tileSize;
						}
					}
					++tilesChecked;
				}

				var sortedChunkIndices = cellList.Keys.OrderBy(i => i).ToArray();
				// group by sequential regions
				int lastIndex = -2;
				var regions = new SortedDictionary<int, int>();
				foreach (var index in sortedChunkIndices)
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
		}
	}
}
