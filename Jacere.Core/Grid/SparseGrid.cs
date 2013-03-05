using System;
using System.Collections.Generic;
using System.Linq;

namespace Jacere.Core
{
	public abstract class SparseGridBase<T> : GridBase
	{
		private readonly T[] m_data;

		protected SparseGridBase(GridDefinition def, int validCellCount)
			: base(def)
		{
			m_data = new T[validCellCount];
		}

		//public abstract void Add();
		//public abstract T Get();
	}

	public class SparseGrid1<T> : SparseGridBase<T>
	{
		private readonly int[] m_index;

		private SparseGrid1(GridDefinition def, int validCellCount)
			: base(def, validCellCount)
		{
			m_index = new int[Def.IndexSize];
		}
	}

	public class SparseGrid2<T> : SparseGridBase<T>
	{
		private readonly Dictionary<int, int> m_index;

		private SparseGrid2(GridDefinition def, int validCellCount)
			: base(def, validCellCount)
		{
			m_index = new Dictionary<int, int>(validCellCount);
		}
	}
}
