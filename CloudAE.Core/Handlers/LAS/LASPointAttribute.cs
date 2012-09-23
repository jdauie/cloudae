using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using CloudAE.Core.Util;

namespace CloudAE.Core.Handlers
{
	/// <summary>
	/// This can be simplified to the first set if that is more clear.
	/// </summary>
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
		private readonly LASPointAttributeDataType m_dataType;
		private readonly byte m_options;
		private readonly string m_name;

		private readonly ulong[] m_noData;
		private readonly ulong[] m_min;
		private readonly ulong[] m_max;

		private readonly double[] m_scale;
		private readonly double[] m_offset;
		private readonly string m_description;

		private readonly Type m_type;
		private readonly int m_typeSize;
		private readonly int m_components;

		public int Size
		{
			get { return IsUndocumented ? m_options : m_typeSize * m_components; }
		}

		public bool IsUndocumented
		{
			get { return (m_type == null); }
		}

		public bool HasNoData
		{
			get { return !IsUndocumented && (m_options & 1) != 0; }
		}

		public bool HasMin
		{
			get { return !IsUndocumented && (m_options & 1 << 1) != 0; }
		}

		public bool HasMax
		{
			get { return !IsUndocumented && (m_options & 1 << 2) != 0; }
		}

		public bool HasScale
		{
			get { return !IsUndocumented && (m_options & 1 << 3) != 0; }
		}

		public bool HasOffset
		{
			get { return !IsUndocumented && (m_options & 1 << 4) != 0; }
		}

		public LASPointExtraBytes(BinaryReader reader)
		{
			reader.ReadBytes(2);
			
			m_dataType = (LASPointAttributeDataType)reader.ReadByte();
			m_options = reader.ReadByte();
			m_name = reader.ReadBytes(32).ToAsciiString();
			
			reader.ReadBytes(4);

			m_noData = reader.ReadUInt64Array(3);
			m_min = reader.ReadUInt64Array(3);
			m_max = reader.ReadUInt64Array(3);

			m_scale = reader.ReadDoubleArray(3);
			m_offset = reader.ReadDoubleArray(3);

			m_description = reader.ReadBytes(32).ToAsciiString();

			m_type = GetTypeFromAttributeDataType(m_dataType);
			m_components = GetComponentCountFromAttributeDataType(m_dataType);

			m_typeSize = (m_type != null) ? SupportedType.GetSize(m_type) : m_options;
		}

		private static Type GetTypeFromAttributeDataType(LASPointAttributeDataType dataType)
		{
			// return the underlying type, even if the actual data is an array
			// (decide later how to clear this up)

			if (!Enum.IsDefined(typeof(LASPointAttributeDataType), dataType) || dataType == LASPointAttributeDataType.Undocumented)
				return null;

			var dataTypeIndex = (LASPointAttributeDataType)((int)dataType % 10);
			switch (dataTypeIndex)
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
			if (!Enum.IsDefined(typeof(LASPointAttributeDataType), dataType) || dataType == LASPointAttributeDataType.Undocumented)
				return 1;

			return ((int)dataType - 1) / 10 + 1;
		}

		#region ISerializeBinary Members

		public void  Serialize(BinaryWriter writer)
		{
 			writer.Write((ushort)0);

			writer.Write((byte)m_dataType);
			writer.Write(m_options);
			writer.Write(m_name.ToAsciiBytes(32));

			writer.Write((uint)0);

			writer.Write(m_noData);
			writer.Write(m_min);
			writer.Write(m_max);

			writer.Write(m_scale);
			writer.Write(m_offset);

			writer.Write(m_description.ToAsciiBytes(32));
		}

		#endregion
}

	public class LASPointAttributeBase
	{
		// byte range
		private readonly int m_startByte;
		private readonly int m_endByte;

		// bit range (for sub-byte values)
		private readonly int m_startBit;
		private readonly int m_endBit;

		// name
		// min
		// max
		// stats

		// valid range? (like scan angle rank -90..90)

		// more info may be needed
		// e.g. GPS time format interpretation depends on LAS header flags


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

	/// <summary>
	/// I might want T to implement an interface that will allow the creation of sub-attributes
	/// (see example below).
	/// The interface might also need to define the translation into "Extra Bytes" which may be 
	/// more limited...and will not have nested attributes.
	/// </summary>
	/// <typeparam name="T"></typeparam>
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

			// I can just ask SupportedType for this
			// but I might want it to be more general
			// ...e.g. a SQuantizedPoint3D attribute has X,Y,Z child attributes
			// thus I would support a tree of arbitrary types
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
