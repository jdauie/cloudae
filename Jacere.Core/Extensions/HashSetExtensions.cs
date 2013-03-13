using System;
using System.Collections.Generic;
using System.Linq;

namespace Jacere.Core
{
	public static class HashSetExtensions
	{
		public static HashSet<T> ToHashSet<T>(this IEnumerable<T> input)
		{
			var set = new HashSet<T>();
			foreach (var value in input)
				set.Add(value);

			return set;
		}

		public static HashSet<T> ToHashSet<T>(this IEnumerable<T> input, HashSet<T> except)
		{
			var set = input.ToHashSet();
			set.ExceptWith(except);

			return set;
		}
	}
}
