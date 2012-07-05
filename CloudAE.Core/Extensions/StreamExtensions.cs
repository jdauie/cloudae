using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using CloudAE.Core.Geometry;

namespace CloudAE.Core
{
	public static class StreamExtensions
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

			try
			{
				obj = constructor.Invoke(new object[] { reader }) as ISerializeBinary;
			}
			catch { }

			return obj;
		}

		public static Extent3D ReadExtent3D(this BinaryReader reader)
		{
			return new Extent3D(reader);
		}

		public static UQuantizedExtent3D ReadUQuantizedExtent3D(this BinaryReader reader)
		{
			return new UQuantizedExtent3D(reader);
		}

		public static PointCloudTileDensity ReadTileDensity(this BinaryReader reader)
		{
			return new PointCloudTileDensity(reader);
		}

		public static UQuantization3D ReadUQuantization3D(this BinaryReader reader)
		{
			return new UQuantization3D(reader);
		}

		public static PointCloudTileSet ReadTileSet(this BinaryReader reader)
		{
			return new PointCloudTileSet(reader);
		}

		public static Statistics ReadStatistics(this BinaryReader reader)
		{
			return new Statistics(reader);
		}

		public static SQuantization3D ReadSQuantization3D(this BinaryReader reader)
		{
			return new SQuantization3D(reader);
		}

		public static LASProjectID ReadLASProjectID(this BinaryReader reader)
		{
			return new LASProjectID(reader);
		}

		public static LASVersionInfo ReadLASVersionInfo(this BinaryReader reader)
		{
			return new LASVersionInfo(reader);
		}

		public static LASGlobalEncoding ReadLASGlobalEncoding(this BinaryReader reader)
		{
			return new LASGlobalEncoding(reader);
		}

		public static LASVLR ReadLASVLR(this BinaryReader reader)
		{
			return new LASVLR(reader);
		}

		public static LASEVLR ReadLASEVLR(this BinaryReader reader)
		{
			return new LASEVLR(reader);
		}

		public static Extent3D ReadLASExtent3D(this BinaryReader reader)
		{
			double hMaxX = reader.ReadDouble();
			double hMinX = reader.ReadDouble();
			double hMaxY = reader.ReadDouble();
			double hMinY = reader.ReadDouble();
			double hMaxZ = reader.ReadDouble();
			double hMinZ = reader.ReadDouble();

			return new Extent3D(hMinX, hMinY, hMinZ, hMaxX, hMaxY, hMaxZ);
		}

		public static LASHeader ReadLASHeader(this BinaryReader reader)
		{
			return new LASHeader(reader);
		}

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
