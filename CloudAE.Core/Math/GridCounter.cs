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
			m_extent = source.Quantization.Convert(m_source.Extent);

			m_gridIndexGenerator = gridIndexGenerator;
			m_gridIndex = grid.Copy<GridIndexCell>();
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
			var tileIndices = new HashSet<PointCloudTileCoord>();
			var lastIndex = PointCloudTileCoord.Empty;

			var pb = chunk.PointDataPtr;
			while (pb < chunk.PointDataEndPtr)
			{
				var p = (SQuantizedPoint3D*)pb;

				var tileIndex = new PointCloudTileCoord(
					(ushort)(((*p).Y - minY) * tilesOverRangeY), 
					(ushort)(((*p).X - minX) * tilesOverRangeX)
				);

				++m_grid.Data[tileIndex.Row, tileIndex.Col];

				// indexing
				if (tileIndex != lastIndex)
				{
					tileIndices.Add(tileIndex);
					lastIndex = tileIndex;
				}

				pb += chunk.PointSizeBytes;
			}

			// update index cells
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

			return chunk;
		}

		public void FinalizeProcess()
		{
			m_grid.CorrectCountOverflow();

			m_gridIndex.CorrectCountOverflow();
			m_gridIndexGenerator.Update(m_source, m_gridIndex, m_grid, m_maxPointCount);
		}
	}
}
