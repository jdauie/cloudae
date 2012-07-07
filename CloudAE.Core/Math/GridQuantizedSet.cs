using System;
using System.Collections.Generic;
using System.Linq;
using CloudAE.Core.Geometry;

namespace CloudAE.Core
{
	public class GridQuantizedSet
	{
		private readonly Grid<float> m_grid;
		private readonly Grid<uint> m_gridQuantized;

		public GridQuantizedSet(Extent2D extent, ushort maxDimension, float fillVal, bool bufferEdge)
		{
			m_grid = new Grid<float>(extent, maxDimension, fillVal, true);
			m_gridQuantized = new Grid<uint>(m_grid.SizeX, m_grid.SizeY, extent, true);
		}
	}
}
