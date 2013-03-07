using System;
using System.Linq;
using Jacere.Core;

namespace CloudAE.Core
{
	public static class GridExtensions
	{
		public static void CorrectCountOverflow(this Grid<GridIndexCell> target)
		{
			var data = target.Data;

			// correct count overflows
			for (int x = 0; x <= target.SizeX; x++)
			{
				data[target.SizeY - 1, x] = new GridIndexCell(data[target.SizeY - 1, x], data[target.SizeY, x]);
				data[target.SizeY, x] = new GridIndexCell();
			}
			for (int y = 0; y < target.SizeY; y++)
			{
				data[y, target.SizeX - 1] = new GridIndexCell(data[y, target.SizeX - 1], data[y, target.SizeX]);
				data[y, target.SizeX] = new GridIndexCell();
			}
		}
	}
}
