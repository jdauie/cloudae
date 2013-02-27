using System;
using System.Collections.Generic;
using System.Linq;

using Jacere.Core.Geometry;

namespace CloudAE.Core
{
	public class Grid<T> : IGrid
	{
		private readonly GridDefinition m_def;

		private readonly T m_fillVal;
		private readonly Extent2D m_extent;

		public readonly T[,] Data;

		#region Properties

		public ushort SizeX
		{
			get { return m_def.SizeX; }
		}

		public ushort SizeY
		{
			get { return m_def.SizeY; }
		}

		public int CellCount
		{
			get { return SizeX * SizeY; }
		}

		public T FillVal
		{
			get { return m_fillVal; }
		}

		public Extent2D Extent
		{
			get { return m_extent; }
		}

		#endregion

		#region Creators

		public static Grid<T> CreateBuffered(ushort sizeX, ushort sizeY, Extent2D extent)
		{
			return CreateBuffered(sizeX, sizeY, extent, default(T));
		}

		public static Grid<T> CreateBuffered(ushort sizeX, ushort sizeY, Extent2D extent, T fillVal)
		{
			var def = GridDefinition.CreateBuffered(sizeX, sizeY);
			return Create(def, extent, fillVal);
		}

		public static Grid<T> Create(Extent2D extent, ushort minDimension, ushort maxDimension, T fillVal)
		{
			var def = GridDefinition.Create(extent, minDimension, maxDimension);
			return Create(def, extent, fillVal);
		}

		public static Grid<T> CreateBuffered(Extent2D extent, ushort minDimension, ushort maxDimension, T fillVal)
		{
			var def = GridDefinition.CreateBuffered(extent, minDimension, maxDimension);
			return Create(def, extent, fillVal);
		}

		private static Grid<T> Create(GridDefinition def, Extent2D extent, T fillVal)
		{
			return new Grid<T>(def, extent, fillVal);
		}

		#endregion

		private Grid(GridDefinition def, Extent2D extent, T fillVal)
		{
			bool fillValIsDefault = EqualityComparer<T>.Default.Equals(fillVal, default(T));
			
			m_def = def;
			m_fillVal = fillVal;
			m_extent = extent;

            Data = new T[m_def.UnderlyingSizeY, m_def.UnderlyingSizeX];

			if (!fillValIsDefault)
				Reset();
		}

		public void Reset()
		{
			T fillVal = FillVal;
			int sizeY = m_def.UnderlyingSizeY;
			int sizeX = m_def.UnderlyingSizeX;

			for (int y = 0; y < sizeY; y++)
				for (int x = 0; x < sizeX; x++)
					Data[y, x] = fillVal;
		}

		public IEnumerable<T> GetCellsInScaledRange(int scaledX, int scaledY, IGrid scaledGrid)
		{
			int startX = (int)Math.Floor(((double)scaledX / scaledGrid.SizeX) * SizeX);
			int startY = (int)Math.Floor(((double)scaledY / scaledGrid.SizeY) * SizeY);

			int endX = (int)Math.Ceiling(((double)(scaledX + 1) / scaledGrid.SizeX) * SizeX);
			int endY = (int)Math.Ceiling(((double)(scaledY + 1) / scaledGrid.SizeY) * SizeY);

			for (int y = startY; y < endY; y++)
				for (int x = startX; x < endX; x++)
					if (!EqualityComparer<T>.Default.Equals(Data[y, x], default(T)))
						yield return Data[y, x];
		}

		public Grid<TNew> Copy<TNew>()
		{
			return new Grid<TNew>(m_def, Extent, default(TNew));
		}
	}
}
