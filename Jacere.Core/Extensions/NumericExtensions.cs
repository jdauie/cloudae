using System;
using System.Linq;
using System.Collections.Generic;

namespace Jacere.Core
{
	public static class NumericExtensions
	{
		public static int MaxIndex<T>(this IEnumerable<T> sequence) where T : IComparable<T>
		{
			int maxIndex = -1;
			T maxValue = default(T);

			int index = 0;
			foreach (T value in sequence)
			{
				if (value.CompareTo(maxValue) > 0 || maxIndex == -1)
				{
					maxIndex = index;
					maxValue = value;
				}
				index++;
			}
			return maxIndex;
		}

		public static long SumLong(this IEnumerable<int> sequence)
		{
			long sum = 0;
			foreach (var v in sequence)
				sum += v;
			return sum;
		}
	}
}
