using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

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

			//int startTileIndex = PointCloudTileCoord.GetIndex(m_grid, m_index);
			//// on the last segment, I actually want to allow overflow to the buffer row.
			//int endTileIndex = PointCloudTileCoord.GetIndex(m_grid, m_index + m_count);

			byte* pb = chunk.PointDataPtr;
			byte* pbDestination = pb;
			while (pb < chunk.PointDataEndPtr)
			{
				var p = (SQuantizedPoint3D*)pb;

                var row = (ushort)(((*p).Y - minY) * tilesOverRangeY);
                var col = (ushort)(((*p).X - minX) * tilesOverRangeX);

				int index = m_grid.Def.GetIndex(row, col);
				if (index >= startIndex && index <= endIndex)
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
	}

	//public class SparseSegment
	//{
	//    private readonly IPointCloudBinarySource m_segment;
	//    private readonly TileRegionFilter m_filter;

	//    public IPointCloudBinarySource Source
	//    {
	//        get { return m_segment; }
	//    }

	//    public TileRegionFilter Filter
	//    {
	//        get { return m_filter; }
	//    }

	//    public SparseSegment(IPointCloudBinarySource segment, TileRegionFilter filter)
	//    {
	//        m_segment = segment;
	//        m_filter = filter;
	//    }
	//}
}
