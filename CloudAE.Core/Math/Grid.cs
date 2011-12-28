﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CloudAE.Core.Geometry;

namespace CloudAE.Core
{
	public class Grid<T>
	{
		public readonly ushort SizeX;
		public readonly ushort SizeY;

		public readonly T[,] Data;

		private T m_fillVal;
		private Extent2D m_extent;

		public int CellCount
		{
			get { return (int)SizeX * SizeY; }
		}

		public T FillVal
		{
			get { return m_fillVal; }
			set { m_fillVal = value; }
		}

		public Extent2D Extent
		{
			get { return m_extent; }
			set { m_extent = value; }
		}

		public Grid(ushort sizeX, ushort sizeY, Extent2D extent, bool bufferEdge)
		{
			SizeX = sizeX;
			SizeY = sizeY;

			m_extent = extent;

			int edgeBufferSize = bufferEdge ? 1 : 0;

			Data = new T[SizeX + edgeBufferSize, SizeY + edgeBufferSize];
		}

		public Grid(Extent2D extent, ushort minDimension, ushort maxDimension, T fillVal, bool bufferEdge)
		{
			FillVal = fillVal;

			SizeX = maxDimension;
			SizeY = maxDimension;

			m_extent = extent;
			double aspect = m_extent.Aspect;
			if (aspect > 1)
				SizeY = (ushort)Math.Max((double)SizeX / aspect, minDimension);
			else
				SizeX = (ushort)Math.Max(SizeX * aspect, minDimension);

			int edgeBufferSize = bufferEdge ? 1 : 0;

			Data = new T[SizeX + edgeBufferSize, SizeY + edgeBufferSize];
			Reset();
		}

		public Grid(Extent2D extent, ushort maxDimension, T fillVal, bool bufferEdge)
			: this(extent, 0, maxDimension, fillVal, bufferEdge)
		{
		}

		public void Reset()
		{
			T fillVal = FillVal;
			int sizeX = Data.GetLength(0);
			int sizeY = Data.GetLength(1);

			for (int x = 0; x < sizeX; x++)
				for (int y = 0; y < sizeY; y++)
					Data[x, y] = fillVal;
		}
	}
}
