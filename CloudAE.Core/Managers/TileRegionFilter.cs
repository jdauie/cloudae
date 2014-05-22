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
        private readonly SQuantizedExtentGrid<int> m_grid;

		private readonly SQuantizedExtent3D m_quantizedExtent;

		public TileRegionFilter(SQuantizedExtentGrid<int> grid, SQuantizedExtent3D quantizedExtent, GridRange tileRange)
		{
			m_range = tileRange;
			m_grid = grid;

			m_quantizedExtent = quantizedExtent;
		}

		public unsafe IPointDataChunk Process(IPointDataChunk chunk)
		{
            // if the end of the range is the last tile in a row, then buffer it.
			var startIndex = m_range.StartPos;
			var endIndex = m_range.EndPos;

			var rowCount = m_grid.Def.SizeY;

			var pb = chunk.PointDataPtr;
			var pbDestination = pb;
			while (pb < chunk.PointDataEndPtr)
			{
				var p = (SQuantizedPoint3D*)pb;

				var row = (((*p).Y - m_quantizedExtent.MinY) / m_grid.CellSizeY);
				var col = (((*p).X - m_quantizedExtent.MinX) / m_grid.CellSizeX);

				// overflow on the end of rows is dealt with by the nature of the index,
				// but overflow after the last row is problematic.
				if (row == rowCount)
					--row;

				var index = m_grid.Def.GetIndex(row, col);
				if (index >= startIndex && index < endIndex)
				{
                    // make copy faster?

                    // assume that most points will be shifted?
                    //if(pb != pbDestination)
                    //{
						for (var i = 0; i < chunk.PointSizeBytes; i++)
							pbDestination[i] = pb[i];
					//}

                    // increment count
                    ++m_grid.Data[row, col];
                    // better would be to do it nearby and add them to the grid at the end (possibly)

					pbDestination += chunk.PointSizeBytes;
				}

				pb += chunk.PointSizeBytes;
			}

			var pointsRemaining = (int)((pbDestination - chunk.PointDataPtr) / chunk.PointSizeBytes);
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
			// create tile position counters (always buffer)
			var tilePositions = new GridBufferPosition[m_grid.SizeY + 1, m_grid.SizeX + 1];
			{
				var index = 0;
				foreach (var tile in GetCellOrdering())
				{
					var count = m_grid.Data[tile.Row, tile.Col];
					var pos = new GridBufferPosition(segmentBuffer, index, count, entrySize);
					tilePositions[tile.Row, tile.Col] = pos;

					index += count;
				}

				// buffer the edges for overflow
				for (var x = 0; x < m_grid.SizeX; x++)
					tilePositions[m_grid.SizeY, x] = tilePositions[m_grid.SizeY - 1, x];
				for (var y = 0; y <= m_grid.SizeY; y++)
					tilePositions[y, m_grid.SizeX] = tilePositions[y, m_grid.SizeX - 1];
			}

			return tilePositions;
		}
	}
}
