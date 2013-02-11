using System;
using System.Collections.Generic;
using System.Linq;

using Jacere.Core.Geometry;
using Jacere.Data.PointCloud;

namespace CloudAE.Core
{
	public unsafe class GridQuantizedSet : IChunkProcess
	{
		private readonly PointCloudTileSource m_source;

		private readonly Grid<float> m_grid;
		private readonly Grid<int> m_gridQuantized;

		private readonly double m_pixelsOverRangeX;
		private readonly double m_pixelsOverRangeY;

		private readonly int m_minX;
		private readonly int m_minY;

		public Grid<float> Grid
		{
			get { return m_grid; }
		}

		public Grid<int> GridQuantized
		{
			get { return m_gridQuantized; }
		}

		//public ushort SizeX
		//{
		//    get { return m_grid.SizeX; }
		//}

		//public ushort SizeY
		//{
		//    get { return m_grid.SizeY; }
		//}

		public GridQuantizedSet(PointCloudTileSource source, ushort maxDimension, float fillVal, bool bufferEdge)
		{
			m_source = source;

			m_grid = new Grid<float>(m_source.Extent, maxDimension, fillVal, true);
			m_gridQuantized = new Grid<int>(m_grid.SizeX, m_grid.SizeY, m_source.Extent, true);

			m_pixelsOverRangeX = (double)m_grid.SizeX / m_source.QuantizedExtent.RangeX;
			m_pixelsOverRangeY = (double)m_grid.SizeY / m_source.QuantizedExtent.RangeY;

			m_minX = m_source.QuantizedExtent.MinX;
			m_minY = m_source.QuantizedExtent.MinY;
		}

		public IPointDataChunk Process(IPointDataChunk chunk)
		{
			byte* pb = chunk.PointDataPtr;
			while (pb < chunk.PointDataEndPtr)
			{
				var p = (SQuantizedPoint3D*)pb;

				int pixelX = (int)(((*p).X - m_minX) * m_pixelsOverRangeX);
				int pixelY = (int)(((*p).Y - m_minY) * m_pixelsOverRangeY);

				if ((*p).Z > m_gridQuantized.Data[pixelX, pixelY])
					m_gridQuantized.Data[pixelX, pixelY] = (*p).Z;

				pb += chunk.PointSizeBytes;
			}

			return chunk;
		}

		public void Populate()
		{
			m_gridQuantized.CorrectMaxOverflow();
			m_gridQuantized.CopyToUnquantized(m_grid, m_source.Quantization, m_source.Extent);
		}
	}
}
