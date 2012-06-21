using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace CloudAE.Core
{
	/// <summary>
	/// Supported records:
	/// User ID/Record ID
	/// LASF_Projection/2111	OGC MATH TRANSFORM WKT RECORD
	/// LASF_Projection/2112	OGC COORDINATE SYSTEM WKT
	/// LASF_Projection/34735	GeoKeyDirectoryTag
	/// LASF_Projection/34736	GeoDoubleParamsTag Record
	/// LASF_Projection/34737	GeoAsciiParamsTag Record
	/// LASF_Spec/0				Classification lookup
	/// LASF_Spec/3				Text area description
	/// LASF_Spec/4				Extra bytes
	/// LASF_Spec/7				Superseded
	/// LASF_Spec/n				Waveform packet descriptor
	///		where 99 < n < 355
	/// </summary>
	public class LASVLR : ISerializeBinary
	{
		private readonly ushort m_reserved;
		private readonly string m_userID;
		private readonly ushort m_recordID;
		private readonly ushort m_recordLengthAfterHeader;
		private readonly string m_description;

		private readonly byte[] m_data;

		public LASVLR(BinaryReader reader)
		{
			m_reserved = reader.ReadUInt16();
			m_userID = reader.ReadBytes(16).UnsafeAsciiBytesToString();
			m_recordID = reader.ReadUInt16();
			m_recordLengthAfterHeader = reader.ReadUInt16();
			m_description = reader.ReadBytes(32).UnsafeAsciiBytesToString();
			m_data = reader.ReadBytes(m_recordLengthAfterHeader);
		}

		public void Serialize(BinaryWriter writer)
		{
			writer.Write(m_reserved);
			writer.Write(m_userID.ToUnsafeAsciiBytes(16));
			writer.Write(m_recordID);
			writer.Write(m_recordLengthAfterHeader);
			writer.Write(m_description.ToUnsafeAsciiBytes(32));
			writer.Write(m_data);
		}
	}
}
