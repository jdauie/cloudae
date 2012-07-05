using System;
using System.Collections.Generic;
using System.Linq;
using CloudAE.Core.Geometry;

namespace CloudAE.Core
{
	public class GridCounter : IDisposable
	{
		private readonly PointCloudBinarySource m_source;
		private readonly Grid<int> m_grid;
		private readonly bool m_quantized;

		private readonly Extent3D m_extent;

		private readonly double m_tilesOverRangeX;
		private readonly double m_tilesOverRangeY;

		public GridCounter(PointCloudBinarySource source, Grid<int> grid)
		{
			m_source = source;
			m_grid = grid;
			m_extent = m_source.Extent;
			m_quantized = (m_source.Quantization != null);

			if (m_quantized)
			{
				var inputQuantization = (SQuantization3D)source.Quantization;
				var q = (SQuantizedExtent3D)inputQuantization.Convert(m_extent);
				m_extent = new Extent3D(q.MinX, q.MinY, q.MinZ, q.MaxX, q.MaxY, q.MaxZ);
			}

			m_tilesOverRangeX = (double)m_grid.SizeX / m_extent.RangeX;
			m_tilesOverRangeY = (double)m_grid.SizeY / m_extent.RangeY;
		}

		public unsafe void Process(IPointDataChunk chunk)
		{
			if (m_quantized)
			{
				byte* pb = chunk.PointDataPtr;
				while (pb < chunk.PointDataEndPtr)
				{
					SQuantizedPoint3D* p = (SQuantizedPoint3D*)pb;

					int tileX = (int)(((*p).X - m_extent.MinX) * m_tilesOverRangeX);
					int tileY = (int)(((*p).Y - m_extent.MinY) * m_tilesOverRangeY);

					//if (tileX < 0) tileX = 0; else if (tileX > m_grid.SizeX) tileX = m_grid.SizeX;
					//if (tileY < 0) tileY = 0; else if (tileY > m_grid.SizeY) tileY = m_grid.SizeY;

					++m_grid.Data[tileX, tileY];

					pb += chunk.PointSizeBytes;
				}
			}
			else
			{
				byte* pb = chunk.PointDataPtr;
				while (pb < chunk.PointDataEndPtr)
				{
					Point3D* p = (Point3D*)pb;
					++m_grid.Data[
						(int)(((*p).X - m_extent.MinX) * m_tilesOverRangeX),
						(int)(((*p).Y - m_extent.MinY) * m_tilesOverRangeY)
					];

					pb += chunk.PointSizeBytes;
				}
			}
		}

		public void Dispose()
		{
			m_grid.CorrectCountOverflow();
		}
	}
}
