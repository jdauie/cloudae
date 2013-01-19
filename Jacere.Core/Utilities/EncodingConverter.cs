using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CloudAE.Core.Util
{
	public static class EncodingConverter
	{
		/// <summary>
		/// RFC 4648
		/// </summary>
		/// <param name="inArray"></param>
		/// <param name="offset"></param>
		/// <param name="length"></param>
		/// <returns></returns>
		public static string ToBase64SafeString(byte[] inArray, int offset, int length)
		{
			string result = Convert.ToBase64String(inArray, offset, length);

			result = result.Replace('+', '-');
			result = result.Replace('/', '_');

			return result;
		}

		public static string ToBase64SafeString(byte[] inArray)
		{
			return ToBase64SafeString(inArray, 0, inArray.Length);
		}
	}
}
