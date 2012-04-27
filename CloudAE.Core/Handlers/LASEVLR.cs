using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace CloudAE.Core
{
	/// <summary>
	/// Additional supported records:
	/// User ID/Record ID
	/// LASF_Spec/65535		Waveform data packets
	/// </summary>
	public class LASEVLR
	{
		private readonly ushort m_reserved;
		private readonly string m_userID;
		private readonly ushort m_recordID;
		private readonly ulong m_recordLengthAfterHeader;
		private readonly string m_description;

		private readonly byte[] m_data;

		public LASEVLR(BinaryReader reader)
		{
			m_reserved = reader.ReadUInt16();
			m_userID = reader.ReadBytes(16).UnsafeAsciiBytesToString();
			m_recordID = reader.ReadUInt16();
			m_recordLengthAfterHeader = reader.ReadUInt64();
			m_description = reader.ReadBytes(32).UnsafeAsciiBytesToString();

			// this data could be massive...that would be strange, but legal
			//m_data = reader.ReadBytes(m_recordLengthAfterHeader);
		}

		public void Serialize(BinaryWriter writer)
		{
			writer.Write(m_reserved);
			writer.Write(m_userID.ToUnsafeAsciiBytes(16));
			writer.Write(m_recordID);
			writer.Write(m_recordLengthAfterHeader);
			writer.Write(m_description.ToUnsafeAsciiBytes(32));
			//writer.Write(m_data);
		}
	}
}
