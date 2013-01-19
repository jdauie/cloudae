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

		/// <summary>
		/// RFC 4648
		/// </summary>
		/// <param name="inArray"></param>
		/// <param name="offset"></param>
		/// <param name="length"></param>
		/// <returns></returns>
		public static string ToBase64SafeString(this byte[] inArray, int offset, int length)
		{
			string result = Convert.ToBase64String(inArray, offset, length);

			result = result.Replace('+', '-');
			result = result.Replace('/', '_');

			return result;
		}

		public static string ToBase64SafeString(this byte[] inArray)
		{
			return ToBase64SafeString(inArray, 0, inArray.Length);
		}
	}
}
