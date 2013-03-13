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

		private readonly GridIndexGenerator m_gridIndexGenerator;
		private readonly Dictionary<int, int[]> m_chunkTiles;

		private int m_maxPointCount;

		public GridCounter(IPointCloudBinarySource source, Grid<int> grid)
			: this(source, grid, null)
		{
		}

		public GridCounter(IPointCloudBinarySource source, Grid<int> grid, GridIndexGenerator gridIndexGenerator)
		{
			m_source = source;
			m_grid = grid;
			m_extent = source.Quantization.Convert(m_source.Extent);

			m_gridIndexGenerator = gridIndexGenerator;
			m_chunkTiles = new Dictionary<int, int[]>();
		}

		public unsafe IPointDataChunk Process(IPointDataChunk chunk)
		{
			if (chunk.PointCount > m_maxPointCount)
				m_maxPointCount = chunk.PointCount;

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

			// update index cells
			var gridIndex = m_grid.Copy<GridIndexCell>();
			foreach (var kvp in m_chunkTiles)
			{
				foreach (var tileIndex in kvp.Value)
				{
					var coord = new PointCloudTileCoord(tileIndex);
					var indexCell = gridIndex.Data[coord.Row, coord.Col];
					if (indexCell == null)
					{
						indexCell = new GridIndexCell();
						gridIndex.Data[coord.Row, coord.Col] = indexCell;
					}
					indexCell.Add(kvp.Key);
				}
			}
			m_chunkTiles.Clear();

			gridIndex.CorrectCountOverflow();
			m_gridIndexGenerator.Update(m_source, gridIndex, m_grid, m_maxPointCount);
		}
	}
}
