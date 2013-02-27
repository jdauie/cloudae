using System;
using System.Collections.Generic;
using System.Linq;

using Jacere.Core.Geometry;

namespace CloudAE.Core
{
	public class Grid<T> : IGrid
	{
		private readonly ushort m_sizeX;
		private readonly ushort m_sizeY;

		public readonly T[,] Data;

		public ushort SizeX
		{
			get { return m_sizeX; }
		}

		public ushort SizeY
		{
			get { return m_sizeY; }
		}

		public int CellCount
		{
			get { return SizeX * SizeY; }
		}

		public T FillVal { get; set; }

		public Extent2D Extent { get; private set; }

		public Grid(ushort sizeX, ushort sizeY, Extent2D extent, bool bufferEdge)
		{
			m_sizeX = sizeX;
			m_sizeY = sizeY;

			Extent = extent;

			int edgeBufferSize = bufferEdge ? 1 : 0;

            Data = new T[SizeY + edgeBufferSize, SizeX + edgeBufferSize];
		}

		public Grid(Extent2D extent, ushort minDimension, ushort maxDimension, T fillVal, bool bufferEdge)
		{
			FillVal = fillVal;

			m_sizeX = maxDimension;
			m_sizeY = maxDimension;

			Extent = extent;
			double aspect = Extent.Aspect;
			if (aspect > 1)
				m_sizeY = (ushort)Math.Max(SizeX / aspect, minDimension);
			else
				m_sizeX = (ushort)Math.Max(SizeX * aspect, minDimension);

			int edgeBufferSize = bufferEdge ? 1 : 0;

            Data = new T[SizeY + edgeBufferSize, SizeX + edgeBufferSize];
			Reset();
		}

		public Grid(Extent2D extent, ushort maxDimension, T fillVal, bool bufferEdge)
			: this(extent, 0, maxDimension, fillVal, bufferEdge)
		{
		}

		public void Reset()
		{
			T fillVal = FillVal;
			int sizeY = Data.GetLength(0);
			int sizeX = Data.GetLength(1);

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
			bool bufferEdge = (Data.GetLength(0) > SizeY);
			return new Grid<TNew>(SizeX, SizeY, Extent, bufferEdge);
		}
	}
}
