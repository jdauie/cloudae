using System;
using System.Collections.Generic;
using System.Linq;

using Jacere.Core.Geometry;

namespace Jacere.Core
{
	public abstract class GridBase : IGridDefinition
	{
		private readonly GridDefinition m_def;

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

		protected GridBase(GridDefinition def)
		{
			m_def = def;
		}
	}

	//public class CountGrid : Grid<int>
	//{
	//	protected CountGrid(GridDefinition def, int fillVal)
	//		: base(def, fillVal)
	//	{
	//	}

	//	public void CorrectCountOverflow()
	//	{
	//		// correct count overflows
	//		for (int x = 0; x <= SizeX; x++)
	//		{
	//			Data[SizeY - 1, x] += Data[SizeY, x];
	//			Data[SizeY, x] = 0;
	//		}
	//		for (int y = 0; y < SizeY; y++)
	//		{
	//			Data[y, SizeX - 1] += Data[y, SizeX];
	//			Data[y, SizeX] = 0;
	//		}
	//	}
	//}

	public class SQuantizedExtentGrid<T> : Grid<T>, IQuantizedExtentGrid
	{
		private readonly int m_cellSizeX;
		private readonly int m_cellSizeY;
		private readonly double m_inverseCellSizeX;
		private readonly double m_inverseCellSizeY;

		#region Properties

		//public double CellSize
		//{
		//	get { return m_cellSize; }
		//}

		public int CellSizeX
		{
			get { return m_cellSizeX; }
		}

		public int CellSizeY
		{
			get { return m_cellSizeY; }
		}

		public double InverseCellSizeX
		{
			get { return m_inverseCellSizeX; }
		}

		public double InverseCellSizeY
		{
			get { return m_inverseCellSizeY; }
		}

		#endregion

		public SQuantizedExtentGrid(GridDefinition def, int cellSizeX, int cellSizeY, T fillVal)
			: base(def, fillVal)
		{
			m_cellSizeX = cellSizeX;
			m_cellSizeY = cellSizeY;
			m_inverseCellSizeX = 1.0 / m_cellSizeX;
			m_inverseCellSizeY = 1.0 / m_cellSizeY;
		}

		public override Grid<TNew> Copy<TNew>(TNew fillVal)
		{
			return new SQuantizedExtentGrid<TNew>(Def, CellSizeX, CellSizeY, fillVal);
		}

		public IEnumerable<SimpleGridCoord> GetCellCoordsInScaledRange(int scaledX, int scaledY, IQuantizedExtentGrid scaledGrid)
		{
			var startX = (ushort)Math.Floor(scaledX * scaledGrid.CellSizeX / (double)CellSizeX);
			var startY = (ushort)Math.Floor(scaledY * scaledGrid.CellSizeY / (double)CellSizeY);

			var endX = (ushort)Math.Ceiling((scaledX + 1) * scaledGrid.CellSizeX / (double)CellSizeX);
			var endY = (ushort)Math.Ceiling((scaledY + 1) * scaledGrid.CellSizeY / (double)CellSizeY);

			if (endX > SizeX) endX = SizeX;
			if (endY > SizeY) endY = SizeY;

			for (var y = startY; y < endY; y++)
				for (var x = startX; x < endX; x++)
					if (!EqualityComparer<T>.Default.Equals(Data[y, x], default(T)))
						yield return new SimpleGridCoord(y, x);
		}
	}

	public class Grid<T> : GridBase
	{
		private readonly T m_fillVal;

		public readonly T[,] Data;

		#region Properties

		public int CellCount
		{
			get { return SizeX * SizeY; }
		}

		public T FillVal
		{
			get { return m_fillVal; }
		}

		#endregion

		#region Creators

		public static Grid<T> Create(ushort sizeX, ushort sizeY, bool buffered = false, T fillVal = default(T))
		{
			var def = new GridDefinition(sizeX, sizeY, buffered);
			return new Grid<T>(def, fillVal);
		}

		#endregion

		protected Grid(GridDefinition def, T fillVal)
			: base(def)
		{
			var fillValIsDefault = EqualityComparer<T>.Default.Equals(fillVal, default(T));

			m_fillVal = fillVal;

			Data = new T[Def.UnderlyingSizeY,Def.UnderlyingSizeX];

			if (!fillValIsDefault)
				Reset();
		}

		public void Reset()
		{
			var fillVal = FillVal;
			int sizeY = Def.UnderlyingSizeY;
			int sizeX = Def.UnderlyingSizeX;

			for (var y = 0; y < sizeY; y++)
				for (var x = 0; x < sizeX; x++)
					Data[y, x] = fillVal;
		}

		public Grid<TNew> Copy<TNew>()
		{
			return Copy(default(TNew));
		}

		public virtual Grid<TNew> Copy<TNew>(TNew fillVal)
		{
			return new Grid<TNew>(Def, fillVal);
		}

		public void ClearOverflow()
		{
			if (Def.UnderlyingSizeX == Def.SizeX)
				throw new Exception("no overflow to clear");

			var data = Data;

			for (var x = 0; x <= SizeX; x++)
				data[SizeY, x] = FillVal;

			for (var y = 0; y < SizeY; y++)
				data[y, SizeX] = FillVal;
		}
	}
}
