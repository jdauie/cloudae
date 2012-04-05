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

		public static string UnsafeAsciiBytesToString(this byte[] buffer)
		{
			int nullLocation = Array.IndexOf<byte>(buffer, 0);
			if (nullLocation > -1)
				return ASCIIEncoding.ASCII.GetString(buffer, 0, nullLocation);
			else
				return ASCIIEncoding.ASCII.GetString(buffer);
		}

	}

}
