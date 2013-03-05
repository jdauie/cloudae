using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using Jacere.Core;
using Jacere.Core.Geometry;
using Jacere.Data.PointCloud;

namespace CloudAE.Core
{
    /// <summary>
    /// I am merging counting into this class for now, 
    /// but I might want to split it into two classes later.
    /// </summary>
	public class TileRegionFilter : IChunkProcess, IFinalizeProcess
	{
		private readonly GridRange m_range;
        private readonly Grid<int> m_grid;

		private readonly SQuantizedExtent3D m_quantizedExtent;

		public TileRegionFilter(Grid<int> grid, SQuantizedExtent3D quantizedExtent, GridRange tileRange)
		{
			m_range = tileRange;
			m_grid = grid;

			m_quantizedExtent = quantizedExtent;
		}

		public unsafe IPointDataChunk Process(IPointDataChunk chunk)
		{
            double minY = m_quantizedExtent.MinY;
            double minX = m_quantizedExtent.MinX;

            double tilesOverRangeX = (double)m_grid.SizeX / m_quantizedExtent.RangeX;
            double tilesOverRangeY = (double)m_grid.SizeY / m_quantizedExtent.RangeY;

			// if the end of the range is the last tile in a row, then buffer it.
			var startIndex = m_range.StartPos;
			var endIndex = m_range.EndPos;

			ushort rowCount = m_grid.Def.SizeY;

			byte* pb = chunk.PointDataPtr;
			byte* pbDestination = pb;
			while (pb < chunk.PointDataEndPtr)
			{
				var p = (SQuantizedPoint3D*)pb;

                var row = (ushort)(((*p).Y - minY) * tilesOverRangeY);
                var col = (ushort)(((*p).X - minX) * tilesOverRangeX);

				// overflow on the end of rows is dealt with by the nature of the index,
				// but overflow after the last row is problematic.
				if (row == rowCount)
					--row;

				int index = m_grid.Def.GetIndex(row, col);
				if (index >= startIndex && index < endIndex)
				{
                    // make copy faster?

                    // assume that most points will be shifted?
                    //if(pb != pbDestination)
                    //{
						for (int i = 0; i < chunk.PointSizeBytes; i++)
							pbDestination[i] = pb[i];
					//}

                    // increment count
                    ++m_grid.Data[row, col];
                    // better would be to do it nearby and add them to the grid at the end (possibly)

					pbDestination += chunk.PointSizeBytes;
				}

				pb += chunk.PointSizeBytes;
			}

			int pointsRemaining = (int)((pbDestination - chunk.PointDataPtr) / chunk.PointSizeBytes);
			return chunk.CreateSegment(pointsRemaining);
		}

        public void FinalizeProcess()
        {
            m_grid.CorrectCountOverflow();
        }

		public IEnumerable<SimpleGridCoord> GetCellOrdering()
		{
			return m_range.GetCellOrdering();
		}

		public GridBufferPosition[,] CreatePositionGrid(IPointDataChunk segmentBuffer, short entrySize)
		{
			// make sure it will fit!

			// since this is only a segment, I have to buffer next to the points 
			// one at a time, rather than at the outside edge.

			// create tile position counters (always buffer)
			var tilePositions = new GridBufferPosition[m_grid.SizeY + 1, m_grid.SizeX + 1];
			{
				int index = 0;
				foreach (var tile in GetCellOrdering())
				{
					int count = m_grid.Data[tile.Row, tile.Col];
					var pos = new GridBufferPosition(segmentBuffer, index, count, entrySize);
					tilePositions[tile.Row, tile.Col] = pos;

					// assign the overflow (this is naive/slow -- most will get overwritten).
					// THIS IS WRONG!
					//tilePositions[tile.Row + 1, tile.Col] = pos;
					//tilePositions[tile.Row, tile.Col + 1] = pos;
					//tilePositions[tile.Row + 1, tile.Col + 1] = pos;

					index += count;
				}

				// buffer the edges for overflow
				for (int x = 0; x < m_grid.SizeX; x++)
					tilePositions[m_grid.SizeY, x] = tilePositions[m_grid.SizeY - 1, x];
				for (int y = 0; y <= m_grid.SizeY; y++)
					tilePositions[y, m_grid.SizeX] = tilePositions[y, m_grid.SizeX - 1];
			}

			return tilePositions;
		}
	}
}
