using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;

namespace Jacere.Core
{
	public static class SerializeBinaryExtensions
	{
		public static void Write(this BinaryWriter writer, ISerializeBinary obj)
		{
			obj.Serialize(writer);
		}

		public static void Write(this BinaryWriter writer, uint[] array)
		{
			for (int i = 0; i < array.Length; i++) writer.Write(array[i]);
		}

		public static void Write(this BinaryWriter writer, ulong[] array)
		{
			for (int i = 0; i < array.Length; i++) writer.Write(array[i]);
		}

		public static void Write(this BinaryWriter writer, double[] array)
		{
			for (int i = 0; i < array.Length; i++) writer.Write(array[i]);
		}

		public static ISerializeBinary ReadObject(this BinaryReader reader, Type type)
		{
			var constructor = type.GetConstructor(new Type[] { typeof(BinaryReader) });

			ISerializeBinary obj = null;

			obj = constructor.Invoke(new object[] { reader }) as ISerializeBinary;

			return obj;
		}

		public static Statistics ReadStatistics(this BinaryReader reader)
		{
			return new Statistics(reader);
		}
	}
}
