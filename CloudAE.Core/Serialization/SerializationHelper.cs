﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace CloudAE.Core
{
	public static class SerializationHelper
	{
		public static ISerializeBinary Clone(ISerializeBinary obj)
		{
			ISerializeBinary clone = null;

			using (var buffer = BufferManager.AcquireBuffer())
			{
				using (var ms = new MemoryStream(buffer.Data, true))
				{
					ms.Position = 0;
					using (var writer = new BinaryWriter(ms))
					{
						writer.Write(obj);
					}
				}
				using (MemoryStream ms = new MemoryStream(buffer.Data))
				{
					ms.Position = 0;
					using (var reader = new BinaryReader(ms))
					{
						clone = reader.ReadObject(obj.GetType());
					}
				}
			}

			return clone;
		}
	}
}