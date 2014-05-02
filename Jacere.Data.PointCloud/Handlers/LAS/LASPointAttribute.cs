using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Jacere.Core;
using Jacere.Core.Util;

namespace Jacere.Data.PointCloud.Handlers
{
	public class LASPointFormatGenerator
	{
		public static void Create(LASHeader header, LASPointExtraBytes[] extra)
		{
			GetAttributes(header.PointDataRecordFormat);

			if (extra != null)
			{
				// convert the extra byte definitions
			}
		}

		public static void GetAttributes(byte pointDataRecordFormat)
		{
			switch (pointDataRecordFormat)
			{
				// 0-10 are known
				case 0:
					//private LASPointFormat_XYZ m_xyz;
					//private ushort m_intensity;
					//private LASPointFormat_Options m_options;
					//private LASPointFormat_Classification m_classification;
					//private sbyte m_scanAngleRank;
					//private byte m_userData;
					//private ushort m_pointSourceID;

					// xyz
					/*
					new LASPointAttributeBase("XYZ", LASPointAttributeDataType.Int3);
					new LASPointAttributeBase("Intensity", LASPointAttributeDataType.UShort);
					new LASPointAttributeBase("Options", LASPointAttributeDataType.Byte);
					new LASPointAttributeBase("Classification", LASPointAttributeDataType.Byte);
					new LASPointAttributeBase("ScanAngleRank", LASPointAttributeDataType.SByte);
					new LASPointAttributeBase("UserData", LASPointAttributeDataType.Byte);
					new LASPointAttributeBase("PointSourceID", LASPointAttributeDataType.UShort);
					*/

					break;
				case 1:
					break;
				case 2:
					break;
			}
		}
	}

	public class LASPointAttributeBase
	{
		// byte range
		//private readonly int m_startByte;
		//private readonly int m_endByte;

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


		private readonly LASPointAttributeDataType m_dataType;
		private readonly string m_name;

		private readonly ulong[] m_noData;
		private readonly ulong[] m_min;
		private readonly ulong[] m_max;

		private readonly double[] m_scale;
		private readonly double[] m_offset;
		private readonly string m_description;

		private readonly Type m_type;
		private readonly int m_typeSize;
		private readonly int m_size;
		private readonly int m_components;

		#region Properties

		public int Size
		{
			get { return m_size; }
		}

		public bool IsUndocumented
		{
			get { return (m_type == null); }
		}

		public bool HasNoData
		{
			get { return (m_noData != null); }
		}

		public bool HasMin
		{
			get { return (m_min != null); }
		}

		public bool HasMax
		{
			get { return (m_max != null); }
		}

		public bool HasScale
		{
			get { return (m_scale != null); }
		}

		public bool HasOffset
		{
			get { return (m_offset != null); }
		}

		#endregion

		public LASPointAttributeBase()
		{
		}

		public LASPointAttributeBase(string name, LASPointAttributeDataType dataType, ulong[] noData, ulong[] min, ulong[] max, double[] scale, double[] offset, string description)
		{
			m_dataType = dataType;
			m_name = name;

			m_noData = noData;
			m_min = min;
			m_max = max;

			m_scale = scale;
			m_offset = offset;

			m_description = description;

			m_type = LASPointExtraBytes.GetTypeFromAttributeDataType(m_dataType);
			m_components = LASPointExtraBytes.GetComponentCountFromAttributeDataType(m_dataType);

			// todo: handle unknown format
			m_typeSize = (m_type != null) ? SupportedType.GetSize(m_type) : 0;// m_options;
			m_size = m_typeSize * m_components;
		}
	}

	/// <summary>
	/// I might want T to implement an interface that will allow the creation of sub-attributes
	/// (see example below).
	/// The interface might also need to define the translation into "Extra Bytes" which may be 
	/// more limited...and will not have nested attributes.
	/// </summary>
	public class LASPointAttribute<T> : LASPointAttributeBase where T : struct
	{
		private T[] m_noData;
		private T[] m_min;
		private T[] m_max;

		public LASPointAttribute()
		{
			
		}

		public LASPointAttribute(string name)
		{
			var actualType = typeof(T);
			var type = actualType.IsEnum ? Enum.GetUnderlyingType(actualType) : actualType;
			var typeCode = Type.GetTypeCode(type);

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
