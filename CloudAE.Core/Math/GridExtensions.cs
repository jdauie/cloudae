using System;
using System.Linq;

using Jacere.Core.Geometry;

namespace CloudAE.Core
{
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
			var data = target.Data;

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
			var data = target.Data;

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
			var data = target.Data;

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
			var data0 = target.Data;
			var data1 = output.Data;

			var scaleFactorZ = (float)quantization.ScaleFactorZ;
			var adjustedOffset = (float)(extent != null ? quantization.OffsetZ - extent.MinZ : quantization.OffsetZ);

			// ">" zero is not quite what I want here
			// it could lose some min values (not important for now)
			for (int y = 0; y < target.SizeY; y++)
				for (int x = 0; x < target.SizeX; x++)
					if (data0[y, x] > 0)
						data1[y, x] = data0[y, x] * scaleFactorZ + adjustedOffset;
		}
	}
}
