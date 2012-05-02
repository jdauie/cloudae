using System;
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

	public static class GridExtensions
	{
		public static void Multiply(this Grid<float> target, float value)
		{
			Multiply(target, value, 0.0f);
		}

		public static void Multiply(this Grid<float> target, float value, float offset)
		{
			float[,] data = target.Data;
			int sizeX = data.GetLength(0);
			int sizeY = data.GetLength(1);

			for (int x = 0; x < sizeX; x++)
				for (int y = 0; y < sizeY; y++)
					data[x, y] = ((data[x, y] - offset) * value) + offset;
		}

		public static void CorrectCountOverflow(this Grid<int> target)
		{
			int[,] data = target.Data;

			// correct count overflows
			for (int x = 0; x <= target.SizeX; x++)
			{
				data[x, target.SizeY - 1] += data[x, target.SizeY];
				data[x, target.SizeY] = 0;
			}
			for (int y = 0; y < target.SizeY; y++)
			{
				data[target.SizeX - 1, y] += data[target.SizeX, y];
				data[target.SizeX, y] = 0;
			}
		}

		public static void CorrectMaxOverflow(this Grid<uint> target)
		{
			uint[,] data = target.Data;

			// correct max overflows
			for (int x = 0; x <= target.SizeX; x++)
			{
				data[x, target.SizeY - 1] = Math.Max(data[x, target.SizeY], data[x, target.SizeY - 1]);
				data[x, target.SizeY] = 0;
			}
			for (int y = 0; y < target.SizeY; y++)
			{
				data[target.SizeX - 1, y] = Math.Max(data[target.SizeX, y], data[target.SizeX - 1, y]);
				data[target.SizeX, y] = 0;
			}
		}

		public static void CopyToUnquantized(this Grid<uint> target, Grid<float> output, Quantization3D quantization, Extent3D extent)
		{
			uint[,] data0 = target.Data;
			float[,] data1 = output.Data;

			float scaleFactorZ = (float)quantization.ScaleFactorZ;
			float adjustedOffset = (float)(extent != null ? quantization.OffsetZ - extent.MinZ : quantization.OffsetZ);

			// ">" zero is not quite what I want here
			// it could lose some min values (not important for now)
			for (int x = 0; x < target.SizeX; x++)
				for (int y = 0; y < target.SizeY; y++)
					if (data0[x, y] > 0)
						data1[x, y] = data0[x, y] * scaleFactorZ + adjustedOffset;
		}
	}
}
