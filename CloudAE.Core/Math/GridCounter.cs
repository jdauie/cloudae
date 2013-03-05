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
				var inputQuantization = source.Quantization;
				var q = inputQuantization.Convert(m_extent);
				m_extent = new Extent3D(q.MinX, q.MinY, q.MinZ, q.MaxX, q.MaxY, q.MaxZ);
			}

			m_tilesOverRangeX = m_grid.SizeX / m_extent.RangeX;
			m_tilesOverRangeY = m_grid.SizeY / m_extent.RangeY;

			if (gridIndexGenerator != null)
			{
				m_gridIndexGenerator = gridIndexGenerator;
				m_gridIndex = grid.Copy<GridIndexCell>();
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
				var lastIndex = PointCloudTileCoord.Empty;

				byte* pb = chunk.PointDataPtr;
				while (pb < chunk.PointDataEndPtr)
				{
					var p = (SQuantizedPoint3D*)pb;

					//ushort tileX = (ushort)(((*p).X - m_extent.MinX) * m_tilesOverRangeX);
					//ushort tileY = (ushort)(((*p).Y - m_extent.MinY) * m_tilesOverRangeY);

					var tileIndex = new PointCloudTileCoord(
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
					var p = (Point3D*)pb;
					++m_grid.Data[
						(int)(((*p).Y - m_extent.MinY) * m_tilesOverRangeY),
						(int)(((*p).X - m_extent.MinX) * m_tilesOverRangeX)
					];

					pb += chunk.PointSizeBytes;
				}
			}

			return chunk;
		}

		public void FinalizeProcess()
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
