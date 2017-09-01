using System;
using System.IO;
using System.Text;

namespace Jacere.Data.PointCloud.Server
{
    public static class VariousExtensions
    {
        public static string ToAsciiString(this byte[] buffer)
        {
            var nullLocation = Array.IndexOf<byte>(buffer, 0);
            return nullLocation > -1 
                ? Encoding.ASCII.GetString(buffer, 0, nullLocation)
                : Encoding.ASCII.GetString(buffer);
        }

        public static uint[] ReadUInt32Array(this BinaryReader reader, int count)
        {
            var array = new uint[count];
            for (var i = 0; i < count; i++)
            {
                array[i] = reader.ReadUInt32();
            }
            return array;
        }

        public static ulong[] ReadUInt64Array(this BinaryReader reader, int count)
        {
            var array = new ulong[count];
            for (var i = 0; i < count; i++)
            {
                array[i] = reader.ReadUInt64();
            }
            return array;
        }

        public static double[] ReadDoubleArray(this BinaryReader reader, int count)
        {
            var array = new double[count];
            for (var i = 0; i < count; i++)
            {
                array[i] = reader.ReadDouble();
            }
            return array;
        }
        
        public static LasProjectId ReadLasProjectId(this BinaryReader reader)
        {
            return new LasProjectId(reader);
        }

        public static LasVersionInfo ReadLasVersionInfo(this BinaryReader reader)
        {
            return new LasVersionInfo(reader);
        }

        public static LasGlobalEncoding ReadLasGlobalEncoding(this BinaryReader reader)
        {
            return new LasGlobalEncoding(reader);
        }

        public static LasVlr ReadLasVlr(this BinaryReader reader)
        {
            return new LasVlr(reader);
        }

        public static LasEvlr ReadLasEvlr(this BinaryReader reader)
        {
            return new LasEvlr(reader);
        }

        public static LasHeader ReadLasHeader(this BinaryReader reader)
        {
            return new LasHeader(reader);
        }

        public static T ReadObject<T>(this BinaryReader reader) where T : class
        {
            var constructor = typeof(T).GetConstructor(new[] { typeof(BinaryReader) });
            return constructor.Invoke(new object[] { reader }) as T;
        }

        public static Point3D ReadPoint3D(this BinaryReader reader)
        {
            return new Point3D(reader);
        }

        public static Extent3D ReadExtent3D(this BinaryReader reader)
        {
            var maxX = reader.ReadDouble();
            var minX = reader.ReadDouble();
            var maxY = reader.ReadDouble();
            var minY = reader.ReadDouble();
            var maxZ = reader.ReadDouble();
            var minZ = reader.ReadDouble();

            return new Extent3D(minX, minY, minZ, maxX, maxY, maxZ);
        }

        public static SQuantizedPoint3D ReadSQuantizedPoint3D(this BinaryReader reader)
        {
            return new SQuantizedPoint3D(reader);
        }

        public static SQuantization3D ReadSQuantization3D(this BinaryReader reader)
        {
            return new SQuantization3D(reader);
        }

        public static int ReadExact(this Stream stream, byte[] buffer, int offset, int count)
        {
            var bytesRemaining = count;
            while (bytesRemaining > 0)
            {
                bytesRemaining -= stream.Read(buffer, offset + (count - bytesRemaining), bytesRemaining);
            }
            return count;
        }
    }

}
