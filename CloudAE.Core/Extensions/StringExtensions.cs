using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CloudAE.Core
{
	public static class StringExtensions
	{
		public static byte[] ToUnsafeAsciiBytes(this string input, int length)
		{
			int inputLength = input.Length;
			if (inputLength > length)
				inputLength = length;

			byte[] buffer = new byte[length];
			ASCIIEncoding.ASCII.GetBytes(input, 0, inputLength, buffer, 0);

			return buffer;
		}
	}
}
