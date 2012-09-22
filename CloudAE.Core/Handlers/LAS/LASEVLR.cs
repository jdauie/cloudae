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
	public class LASEVLR : ISerializeBinary
	{
		private static readonly Dictionary<LASRecordIdentifier, bool> c_knownRecordMapping;

		private readonly ushort m_reserved;
		private readonly string m_userID;
		private readonly ushort m_recordID;
		private readonly ulong m_recordLengthAfterHeader;
		private readonly string m_description;

		private readonly byte[] m_data;

		static LASEVLR()
		{
			c_knownRecordMapping = new Dictionary<LASRecordIdentifier, bool>();
			c_knownRecordMapping.Add(new LASRecordIdentifier("LASF_Spec", 65535), false);
		}

		public LASRecordIdentifier RecordIdentifier
		{
			get { return new LASRecordIdentifier(m_userID, m_recordID); }
		}

		public bool IsKnown
		{
			get
			{
				LASRecordIdentifier record = RecordIdentifier;
				return (LASVLR.IsKnownRecord(record) || c_knownRecordMapping.ContainsKey(record));
			}
		}

		public bool IsInteresting
		{
			get
			{
				LASRecordIdentifier record = RecordIdentifier;
				bool value = LASVLR.IsInterestingRecord(record);
				if (!value)
				{
					if (c_knownRecordMapping.TryGetValue(record, out value))
						return value;
				}

				return value;
			}
		}

		public LASEVLR(BinaryReader reader)
		{
			m_reserved = reader.ReadUInt16();
			m_userID = reader.ReadBytes(16).ToAsciiString();
			m_recordID = reader.ReadUInt16();
			m_recordLengthAfterHeader = reader.ReadUInt64();
			m_description = reader.ReadBytes(32).ToAsciiString();

			// this data could be massive...such as the waveform data packets
			// I should only read records that I want
			// If I later decide that I want to read large records, they should be streamed
			//m_data = reader.ReadBytes(m_recordLengthAfterHeader);

			reader.BaseStream.Seek((long)m_recordLengthAfterHeader, SeekOrigin.Current);
		}

		public void Serialize(BinaryWriter writer)
		{
			writer.Write(m_reserved);
			writer.Write(m_userID.ToAsciiBytes(16));
			writer.Write(m_recordID);
			writer.Write(m_recordLengthAfterHeader);
			writer.Write(m_description.ToAsciiBytes(32));
			//writer.Write(m_data);
		}

		public override string ToString()
		{
			return string.Format("{0} \"{1}\" {2} [{3}]", m_userID, m_description, m_recordID, m_recordLengthAfterHeader);
		}
	}
}
