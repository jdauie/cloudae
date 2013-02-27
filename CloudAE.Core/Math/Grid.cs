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

		private readonly int m_bitsX;
		private readonly int m_bitsY;

		private readonly T m_fillVal;
		private readonly Extent2D m_extent;

		public readonly T[,] Data;

		#region Properties

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

		public T FillVal
		{
			get { return m_fillVal; }
		}

		public Extent2D Extent
		{
			get { return m_extent; }
		}

		private bool Buffered
		{
			get { return (Data.GetLength(0) > SizeY); }
		}

		#endregion

		#region Creators

		public static Grid<T> CreateBuffered(ushort sizeX, ushort sizeY, Extent2D extent)
		{
			return new Grid<T>(sizeX, sizeY, extent, default(T), true);
		}

		public static Grid<T> CreateBuffered(ushort sizeX, ushort sizeY, Extent2D extent, T fillVal)
		{
			return new Grid<T>(sizeX, sizeY, extent, fillVal, true);
		}

		public static Grid<T> Create(Extent2D extent, ushort minDimension, ushort maxDimension, T fillVal, bool bufferEdge)
		{
			ushort sizeX = maxDimension;
			ushort sizeY = maxDimension;

			double aspect = extent.Aspect;
			if (aspect > 1)
				sizeY = (ushort)Math.Max(sizeX / aspect, minDimension);
			else
				sizeX = (ushort)Math.Max(sizeX * aspect, minDimension);

			return new Grid<T>(sizeX, sizeY, extent, fillVal, bufferEdge);
		}

		#endregion

		private Grid(ushort sizeX, ushort sizeY, Extent2D extent, T fillVal, bool bufferEdge)
		{
			bool fillValIsDefault = EqualityComparer<T>.Default.Equals(fillVal, default(T));

			m_fillVal = fillVal;

			m_sizeX = sizeX;
			m_sizeY = sizeY;

			m_bitsX = GetBits(m_sizeX);
			m_bitsY = GetBits(m_sizeY);

			m_extent = extent;

			int edgeBufferSize = bufferEdge ? 1 : 0;

            Data = new T[SizeY + edgeBufferSize, SizeX + edgeBufferSize];

			if (!fillValIsDefault)
				Reset();
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
			return new Grid<TNew>(SizeX, SizeY, Extent, default(TNew), Buffered);
		}

		private static int GetBits(ushort val)
		{
			return (int)Math.Ceiling(Math.Log(val, 2)) + 1;
		}
	}
}
