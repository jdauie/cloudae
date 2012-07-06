using System;
using System.Collections.Generic;
using System.Linq;

namespace CloudAE.Core
{
	public class StreamManager
	{
		public static FileStreamUnbufferedSequentialRead OpenReadStream(string path)
		{
			return OpenReadStream(path, 0);
		}

		public static FileStreamUnbufferedSequentialRead OpenReadStream(string path, long start)
		{
			return new FileStreamUnbufferedSequentialRead(path, start);
		}

		public static FileStreamUnbufferedSequentialWrite OpenWriteStream(string path, long length, long start)
		{
			return new FileStreamUnbufferedSequentialWrite(path, length, start);
		}
	}
}
