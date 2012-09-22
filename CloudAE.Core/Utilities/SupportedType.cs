using System;
using System.Collections.Generic;
using System.Linq;

namespace CloudAE.Core.Util
{
	public class SupportedType
	{
		public readonly Type Type;
		public readonly TypeCode TypeCode;
		public readonly int Size;

		private SupportedType(Type type, TypeCode typeCode, int size)
		{
			Type = type;
			TypeCode = typeCode;
			Size = size;
		}

		public static SupportedType GetType<T>()
		{
			Type type = typeof(T);
			TypeCode typeCode = Type.GetTypeCode(type);
			int size = GetSize(typeCode);

			return new SupportedType(type, typeCode, size);
		}

		public static int GetSize(Type type)
		{
			return GetSize(Type.GetTypeCode(type));
		}

		public static int GetSize(TypeCode typeCode)
		{
			switch (typeCode)
			{
				case TypeCode.Byte:   return sizeof(byte);
				case TypeCode.SByte:  return sizeof(sbyte);
				case TypeCode.Int16:  return sizeof(short);
				case TypeCode.UInt16: return sizeof(ushort);
				case TypeCode.Int32:  return sizeof(int);
				case TypeCode.UInt32: return sizeof(uint);
				case TypeCode.Int64:  return sizeof(long);
				case TypeCode.UInt64: return sizeof(ulong);
				case TypeCode.Single: return sizeof(float);
				case TypeCode.Double: return sizeof(double);

				default:
					throw new NotSupportedException();
			}
		}
	}
}
