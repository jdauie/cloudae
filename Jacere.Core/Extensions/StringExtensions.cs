using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Jacere.Core
{
	public static class StringExtensions
	{
		public static byte[] ToAsciiBytes(this string input, int length)
		{
			int inputLength = input.Length;
			if (inputLength > length)
				inputLength = length;

			byte[] buffer = new byte[length];
			Encoding.ASCII.GetBytes(input, 0, inputLength, buffer, 0);

			return buffer;
		}
	}
}
