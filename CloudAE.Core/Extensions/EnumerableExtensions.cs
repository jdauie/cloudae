using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CloudAE.Core
{
	/// <summary>
	/// http://gregbeech.com/blog/from-projections-to-comparers
	/// </summary>
	public static class EnumerableExtensions
	{
		public static IEnumerable<T> Distinct<T, TKey>(
			this IEnumerable<T> source,
			Func<T, TKey> keySelector)
		{
			var comparer = new KeyEqualityComparer<T, TKey>(keySelector);
			return source.Distinct(comparer);
		}

		public static IEnumerable<T> Distinct<T, TKey>(
			this IEnumerable<T> source,
			Func<T, TKey> keySelector,
			IEqualityComparer<TKey> keyEqualityComparer)
		{
			var comparer = new KeyEqualityComparer<T, TKey>(keySelector, keyEqualityComparer);
			return source.Distinct(comparer);
		}
	}

	public sealed class KeyEqualityComparer<T, TKey> : IEqualityComparer<T>
	{
		private readonly IEqualityComparer<TKey> equalityComparer;
		private readonly Func<T, TKey> keySelector;

		public KeyEqualityComparer(Func<T, TKey> keySelector)
			: this(keySelector, EqualityComparer<TKey>.Default)
		{
		}

		public KeyEqualityComparer(Func<T, TKey> keySelector, IEqualityComparer<TKey> equalityComparer)
		{
			this.keySelector = keySelector;
			this.equalityComparer = equalityComparer;
		}

		public bool Equals(T x, T y)
		{
			return this.equalityComparer.Equals(this.keySelector(x), this.keySelector(y));
		}

		public int GetHashCode(T obj)
		{
			return this.equalityComparer.GetHashCode(this.keySelector(obj));
		}
	}
}
