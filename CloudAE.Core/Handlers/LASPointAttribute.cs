using System;
using System.Collections.Generic;
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

	public class LASPointAttributeBase
	{
		private LASPointAttributeDataType m_dataType;
		private byte m_options;
		private string m_name;

		private long[] m_noData;
		private long[] m_min;
		private long[] m_max;

		private double[] m_scale;
		private double[] m_offset;
		private string m_description;

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
				case TypeCode.Int16:
				case TypeCode.UInt16:
				case TypeCode.Int32:
				case TypeCode.Int64:
				case TypeCode.UInt32:
				case TypeCode.Single:
				case TypeCode.UInt64:
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
