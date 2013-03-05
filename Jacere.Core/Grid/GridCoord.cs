using System;
using System.Collections.Generic;
using System.Linq;

namespace Jacere.Core
{
	public struct SimpleGridCoord// : ITileCoord
	{
		private readonly ushort m_row;
		private readonly ushort m_col;

		public ushort Row
		{
			get { return m_row; }
		}

		public ushort Col
		{
			get { return m_col; }
		}

		public SimpleGridCoord(ushort y, ushort x)
		{
			m_row = y;
			m_col = x;
		}

		public override string ToString()
		{
			return string.Format("({0}, {1})", m_row, m_col);
		}
	}

	public class GridCoord
	{
		private readonly GridDefinition m_def;
		private readonly ushort m_row;
		private readonly ushort m_col;

		public GridDefinition Def
		{
			get { return m_def; }
		}

		public ushort Row
		{
			get { return m_row; }
		}

		public ushort Col
		{
			get { return m_col; }
		}

		public int Index
		{
			get { return m_def.GetIndex(m_row, m_col); }
		}

		public GridCoord(GridDefinition def, ushort row, ushort col)
		{
			m_def = def;
			m_row = row;
			m_col = col;
		}

		public static GridCoord operator +(GridCoord c1, int c2)
		{
			var row = c1.m_row;
			var col = c1.m_col + 1;

			if (col == c1.m_def.SizeX)
			{
				++row;
				col = 0;
			}

			return new GridCoord(c1.m_def, row, (ushort)col);
		}
	}

	public class GridRange
	{
		private readonly GridCoord m_start;
		private readonly GridCoord m_end;

		public int StartPos
		{
			get { return m_start.Index; }
		}

		public int EndPos
		{
			get { return (m_end + 1).Index; }
		}

		public GridRange(GridCoord startIndex, GridCoord endIndex)
		{
			m_start = startIndex;
			m_end = endIndex;
		}

		public IEnumerable<SimpleGridCoord> GetCellOrdering()
		{
			for (var y = m_start.Row; y <= m_end.Row; y++)
			{
				var x = (y == m_start.Row) ? m_start.Col : (ushort)0;
				var endX = (y == m_end.Row) ? m_end.Col : m_end.Def.SizeX;

				for (; x < endX; x++)
					yield return new SimpleGridCoord(y, x);
			}
		}
	}
}
