﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace Jacere.Core
{
	public static class SerializationHelper
	{
		public static byte[] Serialize(ISerializeBinary obj)
		{
			// todo: I should probably throw here
			if (obj == null)
				return new byte[0];

			using (var ms = new MemoryStream())
			{
				ms.Position = 0;
				using (var writer = new BinaryWriter(ms))
				{
					writer.Write(obj);
				}
				return ms.GetBuffer();
			}
		}

		public static T Deserialize<T>(byte[] buffer) where T : class, ISerializeBinary
		{
			using (var ms = new MemoryStream(buffer))
			{
				ms.Position = 0;
				using (var reader = new BinaryReader(ms))
				{
					return reader.ReadObject<T>();
				}
			}
		}

		public static T Clone<T>(T obj) where T : class, ISerializeBinary
		{
			T clone;

			// todo: don't use AcquireBuffer here!
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
				using (var ms = new MemoryStream(buffer.Data))
				{
					ms.Position = 0;
					using (var reader = new BinaryReader(ms))
					{
						clone = reader.ReadObject<T>();
					}
				}
			}

			return clone;
		}
	}
}
