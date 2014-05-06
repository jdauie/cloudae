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

		public static T ReadObject<T>(this BinaryReader reader) where T : class, ISerializeBinary
		{
			var constructor = typeof(T).GetConstructor(new [] { typeof(BinaryReader) });
			return constructor.Invoke(new object[] { reader }) as T;
		}

		public static Statistics ReadStatistics(this BinaryReader reader)
		{
			return new Statistics(reader);
		}
	}
}
