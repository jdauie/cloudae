using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Jacere.Core.Geometry;

namespace Jacere.Core
{
	public interface IChunkProcess
	{
		IPointDataChunk Process(IPointDataChunk chunk);
	}

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

	public class TileRegionFilter : IChunkProcess
	{
		private readonly int m_index;
		private readonly int m_count;
		private readonly IGrid m_grid;

		private readonly PointCloudTileCoord[] m_tiles;
		private readonly HashSet<int> m_tileLookup;

		private readonly double m_tilesOverRangeX;
		private readonly double m_tilesOverRangeY;
		private readonly UQuantizedExtent3D m_quantizedExtent;

		public TileRegionFilter(IGrid grid, UQuantizedExtent3D quantizedExtent, int tileIndex, int count)
		{
			m_index = tileIndex;
			m_count = count;
			m_grid = grid;

			m_tiles = PointCloudTileSet.GetTileOrdering(m_grid).Skip(m_index).Take(m_count).ToArray();
			m_tileLookup = new HashSet<int>(m_tiles.Select(t => t.Index));

			m_quantizedExtent = quantizedExtent;
			m_tilesOverRangeX = (double)m_grid.SizeX / m_quantizedExtent.RangeX;
			m_tilesOverRangeY = (double)m_grid.SizeY / m_quantizedExtent.RangeY;
		}

		public unsafe IPointDataChunk Process(IPointDataChunk chunk)
		{
			byte* pb = chunk.PointDataPtr;
			byte* pbDestination = pb;
			while (pb < chunk.PointDataEndPtr)
			{
				UQuantizedPoint3D* p = (UQuantizedPoint3D*)pb;

				int index = PointCloudTileCoord.GetIndex(
					(ushort)(((double)(*p).Y - m_quantizedExtent.MinY) * m_tilesOverRangeY),
					(ushort)(((double)(*p).X - m_quantizedExtent.MinX) * m_tilesOverRangeX)
				);

				if (m_tileLookup.Contains(index))
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

			int pointsRemaining = (int)((pbDestination - chunk.PointDataPtr) / chunk.PointSizeBytes);
			return chunk.CreateSegment(pointsRemaining);
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
