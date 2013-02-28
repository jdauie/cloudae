using System;
using System.Linq;

namespace CloudAE.Core
{
	public class GridCoord
	{
		private readonly GridDefinition m_def;
		private readonly ushort m_row;
		private readonly ushort m_col;

		public GridCoord(IGridDefinition grid, ushort row, ushort col)
		{
			m_def = grid.Def;
			m_row = row;
			m_col = col;
		}
	}

	public class GridRange
	{
		private readonly GridDefinition m_def;
		private readonly GridCoord m_start;
		private readonly ushort m_row;

		public GridRange(IGridDefinition grid, int incrementalStartIndex, int count)
		{
			m_def = grid.Def;
			//m_start = m_def.GetIndex(incrementalStartIndex);
		}
	}
}
