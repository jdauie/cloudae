using System;
using System.Collections.Generic;
using System.Linq;

using Jacere.Core.Geometry;

namespace CloudAE.Core
{
	public abstract class SparseGridBase<T> : IGrid
	{
		private readonly GridDefinition m_def;

		private readonly T[] m_data;

		#region Properties

		public GridDefinition Def
		{
			get { return m_def; }
		}

		public ushort SizeX
		{
			get { return m_def.SizeX; }
		}

		public ushort SizeY
		{
			get { return m_def.SizeY; }
		}

		#endregion

		protected SparseGridBase(ushort sizeX, ushort sizeY, int validCellCount)
		{
			m_def = GridDefinition.Create(sizeX, sizeY);

			m_data = new T[validCellCount];
		}

		//public abstract void Add();
		//public abstract T Get();
	}

	public class SparseGrid1<T> : SparseGridBase<T>
	{
		private readonly int[] m_index;

		private SparseGrid1(ushort sizeX, ushort sizeY, int validCellCount)
			: base(sizeX, sizeY, validCellCount)
		{
			m_index = new int[Def.IndexSize];
		}
	}

	public class SparseGrid2<T> : SparseGridBase<T>
	{
		private readonly Dictionary<int, int> m_index;

		private SparseGrid2(ushort sizeX, ushort sizeY, int validCellCount)
			: base(sizeX, sizeY, validCellCount)
		{
			m_index = new Dictionary<int, int>(validCellCount);
		}
	}
}
