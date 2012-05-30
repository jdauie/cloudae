using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CloudAE.Core
{
	public static class ArrayExtensions
	{
		public static IEnumerable<T> ToEnumerable<T>(this Array target)
		{
			foreach (var item in target)
				yield return (T)item;
		}

		public static string UnsafeAsciiBytesToString(this byte[] buffer)
		{
			int nullLocation = Array.IndexOf<byte>(buffer, 0);
			if (nullLocation > -1)
				return ASCIIEncoding.ASCII.GetString(buffer, 0, nullLocation);
			else
				return ASCIIEncoding.ASCII.GetString(buffer);
		}

		public static void ParallelSort(this int[] target)
		{
			int bucketCountPow = 2;
			int bucketCount = (int)Math.Pow(2, bucketCountPow);

			// get range for shifting
			int min = target[0];
			int max = target[0];
			for (int i = 0; i < target.Length; i++)
				if (target[i] < min) min = target[i]; else if (target[i] > max) max = target[i];
			long range = (long)max - min;
			int rangePowCeil = (int)Math.Ceiling(Math.Log(range, 2));
			int bucketCountShift = rangePowCeil - bucketCountPow;
			int minShifted = min >> bucketCountShift;

			// determine bucket sizes
			int[] bucketCounts = new int[bucketCount + 1];
			for (int i = 0; i < target.Length; i++)
				++bucketCounts[(target[i] >> bucketCountShift) - minShifted];

			int[][] buckets = new int[bucketCount + 1][];
			for (int b = 0; b < buckets.Length; b++)
				buckets[b] = new int[bucketCounts[b]];

			int[] bucketPositions = new int[bucketCount + 1];

			// copy points to buckets
			for (int i = 0; i < target.Length; i++)
			{
				int bucket = (target[i] >> bucketCountShift) - minShifted;
				buckets[bucket][bucketPositions[bucket]++] = target[i];
			}

			Parallel.ForEach(buckets.Where(b => b.Length > 0), Array.Sort<int>);

			// copy back
			int position = 0;
			for (int b = 0; b < buckets.Length; b++)
			{
				Array.Copy(buckets[b], 0, target, position, buckets[b].Length);
				position += buckets[b].Length;
			}
		}
	}

}
