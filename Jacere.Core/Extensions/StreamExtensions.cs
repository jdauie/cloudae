using System;
using System.IO;

namespace CloudAE.Core
{
	public static class SerializeArrayExtensions
	{
		public static uint[] ReadUInt32Array(this BinaryReader reader, int count)
		{
			var array = new uint[count];
			for (int i = 0; i < count; i++) array[i] = reader.ReadUInt32();
			return array;
		}

		public static ulong[] ReadUInt64Array(this BinaryReader reader, int count)
		{
			var array = new ulong[count];
			for (int i = 0; i < count; i++) array[i] = reader.ReadUInt64();
			return array;
		}

		public static double[] ReadDoubleArray(this BinaryReader reader, int count)
		{
			var array = new double[count];
			for (int i = 0; i < count; i++) array[i] = reader.ReadDouble();
			return array;
		}
	}
}
