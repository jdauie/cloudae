using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CloudAE.Core
{
	public static class ArrayExtensions
	{
		public static IEnumerable<T> ToEnumerable<T>(this Array target)
		{
			foreach (var item in target)
				yield return (T)item;
		}
	}

}
