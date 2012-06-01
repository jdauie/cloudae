using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace CloudAE.Core.Handlers
{
	public enum LASPointAttributeDataType : byte
	{
		Undocumented,
		Byte, // 1
		SByte,
		UShort,
		Short,
		UInt,
		Int,
		ULong,
		Long,
		Single,
		Double,
		Byte2, // 11
		SByte2,
		UShort2,
		Short2,
		UInt2,
		Int2,
		ULong2,
		Long2,
		Float2,
		Double2,
		Byte3, // 21
		SByte3,
		UShort3,
		Short3,
		UInt3,
		Int3,
		ULong3,
		Long3,
		Float3,
		Double3 // 30
	}

	public class LASPointExtraBytes : ISerializeBinary
	{
		private LASPointAttributeDataType m_dataType;
		private byte m_options;
		private string m_name;

		private ulong[] m_noData;
		private ulong[] m_min;
		private ulong[] m_max;

		private double[] m_scale;
		private double[] m_offset;
		private string m_description;

		private Type m_type;
		private int m_components;

		public LASPointExtraBytes(BinaryReader reader)
		{
			reader.ReadBytes(2);
			
			m_dataType = (LASPointAttributeDataType)reader.ReadByte();
			m_options = reader.ReadByte();
			m_name = reader.ReadBytes(32).UnsafeAsciiBytesToString();
			
			reader.ReadBytes(4);

			m_noData = reader.ReadUInt64Array(3);
			m_min = reader.ReadUInt64Array(3);
			m_max = reader.ReadUInt64Array(3);

			m_scale = reader.ReadDoubleArray(3);
			m_offset = reader.ReadDoubleArray(3);

			m_description = reader.ReadBytes(32).UnsafeAsciiBytesToString();

			m_type = GetTypeFromAttributeDataType(m_dataType);
			m_components = GetComponentCountFromAttributeDataType(m_dataType);
		}

		private static Type GetTypeFromAttributeDataType(LASPointAttributeDataType dataType)
		{
			switch (dataType)
			{
				case LASPointAttributeDataType.Byte:   return typeof(Byte);
				case LASPointAttributeDataType.SByte:  return typeof(SByte);
				case LASPointAttributeDataType.UShort: return typeof(UInt16);
				case LASPointAttributeDataType.Short:  return typeof(Int16);
				case LASPointAttributeDataType.UInt:   return typeof(UInt32);
				case LASPointAttributeDataType.Int:    return typeof(Int32);
				case LASPointAttributeDataType.ULong:  return typeof(UInt64);
				case LASPointAttributeDataType.Long:   return typeof(Int64);
				case LASPointAttributeDataType.Single: return typeof(Single);
				case LASPointAttributeDataType.Double: return typeof(Double);
			}

			return null;
		}

		private static int GetComponentCountFromAttributeDataType(LASPointAttributeDataType dataType)
		{
			if (dataType == LASPointAttributeDataType.Undocumented)
				return 0;

			return ((int)dataType - 1) / 10;
		}

		#region ISerializeBinary Members

		public void  Serialize(BinaryWriter writer)
		{
 			writer.Write((ushort)0);

			writer.Write((byte)m_dataType);
			writer.Write(m_options);
			writer.Write(m_name.ToUnsafeAsciiBytes(32));

			writer.Write((uint)0);

			writer.Write(m_noData);
			writer.Write(m_min);
			writer.Write(m_max);

			writer.Write(m_scale);
			writer.Write(m_offset);

			writer.Write(m_description.ToUnsafeAsciiBytes(32));
		}

		#endregion
}

	public class LASPointAttributeBase
	{
		// maybe have two different classes for "Attribute" and "Extra Bytes"
		// they aren't necessarily the same structure

		private LASPointAttributeDataType m_dataType;
		private byte m_options;
		private string m_name;

		private long[] m_noData;
		private long[] m_min;
		private long[] m_max;

		private double[] m_scale;
		private double[] m_offset;
		private string m_description;

		private Type m_type;
		private int m_components;

		public LASPointAttributeBase()
		{

		}
	}

	public class LASPointAttribute<T> : LASPointAttributeBase where T : struct
	{
		private T[] m_noData;
		private T[] m_min;
		private T[] m_max;

		public LASPointAttribute()
		{
			Type actualType = typeof(T);
			Type type = actualType.IsEnum ? Enum.GetUnderlyingType(actualType) : actualType;
			TypeCode typeCode = Type.GetTypeCode(type);

			switch (typeCode)
			{
				case TypeCode.Byte:
				case TypeCode.SByte:
				case TypeCode.UInt16:
				case TypeCode.Int16:
				case TypeCode.UInt32:
				case TypeCode.Int32:
				case TypeCode.UInt64:
				case TypeCode.Int64:
				case TypeCode.Single:
				case TypeCode.Double:
					break;
				default:
					throw new InvalidOperationException("Unsupported attribute type.");
			}

		}
	}

	public class LASPointAttributeSet
	{
		private readonly List<LASPointAttributeBase> m_attributes;

		public LASPointAttributeSet()
		{
			m_attributes = new List<LASPointAttributeBase>();
		}

		public void Add(LASPointAttributeBase attribute)
		{
			m_attributes.Add(attribute);
		}
	}
}
