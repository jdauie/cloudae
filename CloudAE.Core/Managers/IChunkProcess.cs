using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using CloudAE.Core.Geometry;

namespace CloudAE.Core
{
	public interface IChunkProcess
	{
		IPointDataChunk Process(IPointDataChunk chunk);
	}

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

	public class TileRegionFilter : IChunkProcess
	{
		private readonly PointCloudTileCoord m_index;
		private readonly int m_count;
		private readonly double m_tilesOverRangeX;
		private readonly double m_tilesOverRangeY;
		private readonly UQuantizedExtent3D m_quantizedExtent;

		public TileRegionFilter(IGrid grid, UQuantizedExtent3D quantizedExtent, uint tileIndex, int count)
		{
			m_index = new PointCloudTileCoord(tileIndex);
			m_count = count;

			m_quantizedExtent = quantizedExtent;
			m_tilesOverRangeX = (double)grid.SizeX / quantizedExtent.RangeX;
			m_tilesOverRangeY = (double)grid.SizeY / quantizedExtent.RangeY;
		}

		public unsafe IPointDataChunk Process(IPointDataChunk chunk)
		{
			int tileMin = m_index.Index;
			int tileMax = tileMin + m_count;

			byte* pb = chunk.PointDataPtr;
			byte* pbDestination = pb;
			while (pb < chunk.PointDataEndPtr)
			{
				UQuantizedPoint3D* p = (UQuantizedPoint3D*)pb;

				int index = PointCloudTileCoord.GetIndex(
					(int)(((double)(*p).Y - m_quantizedExtent.MinY) * m_tilesOverRangeY),
					(int)(((double)(*p).X - m_quantizedExtent.MinX) * m_tilesOverRangeX)
				);

				if(index >= tileMin && index < tileMax)
				{
					if(pb != pbDestination)
					{
						for (int i = 0; i < chunk.PointSizeBytes; i++)
							pbDestination[i] = pb[i];
					}

					pbDestination += chunk.PointSizeBytes;
				}

				pb += chunk.PointSizeBytes;
			}

			return chunk;
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
