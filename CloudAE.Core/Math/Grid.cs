using System;
using System.Collections.Generic;
using System.Linq;

using Jacere.Core.Geometry;

namespace CloudAE.Core
{
	public interface IGrid
	{
		ushort SizeX { get; }
		ushort SizeY { get; }
	}

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

			for (int y = 0; y < sizeY; y++)
				for (int x = 0; x < sizeX; x++)
					data[y, x] = ((data[y, x] - offset) * value) + offset;
		}

		public static void CorrectCountOverflow(this Grid<GridIndexCell> target)
		{
			GridIndexCell[,] data = target.Data;

            // correct count overflows
            for (int y = 0; y < target.SizeY; y++)
            {
                data[y, target.SizeX - 1] = new GridIndexCell(data[y, target.SizeX - 1], data[y, target.SizeX]);
                data[y, target.SizeX] = new GridIndexCell();
            }
			for (int x = 0; x <= target.SizeX; x++)
			{
                data[target.SizeY - 1, x] = new GridIndexCell(data[target.SizeY - 1, x], data[target.SizeY, x]);
				data[target.SizeY, x] = new GridIndexCell();
			}
		}

		public static void CorrectCountOverflow(this Grid<int> target)
		{
			int[,] data = target.Data;

            // correct count overflows
            for (int y = 0; y < target.SizeY; y++)
            {
				data[y, target.SizeX - 1] += data[y, target.SizeX];
                data[y, target.SizeX] = 0;
            }
			for (int x = 0; x <= target.SizeX; x++)
			{
				data[target.SizeY - 1, x] += data[target.SizeY, x];
				data[target.SizeY, x] = 0;
			}
		}

		public static void CorrectMaxOverflow(this Grid<int> target)
		{
			int[,] data = target.Data;

            // correct max overflows
            for (int y = 0; y < target.SizeY; y++)
            {
				data[y, target.SizeX - 1] = Math.Max(data[y, target.SizeX], data[y, target.SizeX - 1]);
				data[y, target.SizeX] = 0;
            }
			for (int x = 0; x <= target.SizeX; x++)
			{
				data[target.SizeY - 1, x] = Math.Max(data[target.SizeY, x], data[target.SizeY - 1, x]);
				data[target.SizeY, x] = 0;
			}
		}

		public static void CopyToUnquantized(this Grid<int> target, Grid<float> output, Quantization3D quantization, Extent3D extent)
		{
			int[,] data0 = target.Data;
			float[,] data1 = output.Data;

			float scaleFactorZ = (float)quantization.ScaleFactorZ;
			float adjustedOffset = (float)(extent != null ? quantization.OffsetZ - extent.MinZ : quantization.OffsetZ);

			// ">" zero is not quite what I want here
			// it could lose some min values (not important for now)
			for (int y = 0; y < target.SizeY; y++)
				for (int x = 0; x < target.SizeX; x++)
					if (data0[y, x] > 0)
						data1[y, x] = data0[y, x] * scaleFactorZ + adjustedOffset;
		}
	}
}
