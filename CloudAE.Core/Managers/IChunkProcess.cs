using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using Jacere.Core.Geometry;
using Jacere.Data.PointCloud;

namespace CloudAE.Core
{
	/// <summary>
	/// Processing stack.
	/// </summary>
	public class ChunkProcessSet : IChunkProcess
	{
		private readonly List<IChunkProcess> m_chunkProcesses;

		public ChunkProcessSet(params IChunkProcess[] chunkProcesses)
		{
			m_chunkProcesses = new List<IChunkProcess>();
			foreach (var chunkProcess in chunkProcesses)
				if (chunkProcess != null)
					m_chunkProcesses.Add(chunkProcess);
		}

		public IPointDataChunk Process(IPointDataChunk chunk)
		{
			// allow filters to replace the chunk definition
			var currentChunk = chunk;
			foreach (var chunkProcess in m_chunkProcesses)
				currentChunk = chunkProcess.Process(currentChunk);

			return currentChunk;
		}
	}

    /// <summary>
    /// I am merging counting into this class for now, 
    /// but I might want to split it into two classes later.
    /// </summary>
	public class TileRegionFilter : IChunkProcess, IDisposable
	{
		private readonly int m_index;
		private readonly int m_count;
        private readonly Grid<int> m_grid;

		//private readonly PointCloudTileCoord[] m_tiles;
		//private readonly HashSet<int> m_tileLookup;

		//private readonly double m_tilesOverRangeX;
		//private readonly double m_tilesOverRangeY;
		private readonly SQuantizedExtent3D m_quantizedExtent;

		public TileRegionFilter(Grid<int> grid, SQuantizedExtent3D quantizedExtent, int tileIndex, int count)
		{
            // this index is just an incremental index, not a coord index.
			m_index = tileIndex;
			m_count = count;
			m_grid = grid;

            // I am removed tree-ordering, so I don't need to be this elaborate
			//m_tiles = PointCloudTileSet.GetTileOrdering(m_grid).Skip(m_index).Take(m_count).ToArray();
			//m_tileLookup = new HashSet<int>(m_tiles.Select(t => t.Index));

			m_quantizedExtent = quantizedExtent;
			//m_tilesOverRangeX = (double)m_grid.SizeX / m_quantizedExtent.RangeX;
			//m_tilesOverRangeY = (double)m_grid.SizeY / m_quantizedExtent.RangeY;
		}

		public unsafe IPointDataChunk Process(IPointDataChunk chunk)
		{
            double minY = m_quantizedExtent.MinY;
            double minX = m_quantizedExtent.MinX;

            double tilesOverRangeX = (double)m_grid.SizeX / m_quantizedExtent.RangeX;
            double tilesOverRangeY = (double)m_grid.SizeY / m_quantizedExtent.RangeY;

            int startTileIndex = PointCloudTileCoord.GetIndex(m_grid, m_index);
            int endTileIndex = PointCloudTileCoord.GetIndex(m_grid, m_index + m_count);

			byte* pb = chunk.PointDataPtr;
			byte* pbDestination = pb;
			while (pb < chunk.PointDataEndPtr)
			{
				var p = (SQuantizedPoint3D*)pb;

                var row = (ushort)(((*p).Y - minY) * tilesOverRangeY);
                var col = (ushort)(((*p).X - minX) * tilesOverRangeX);

				int index = PointCloudTileCoord.GetIndex(row, col);

				if (index >= startTileIndex && index < endTileIndex)
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

        public void Dispose()
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
