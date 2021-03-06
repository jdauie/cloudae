﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Jacere.Core;

namespace Jacere.Data.PointCloud
{
	/// <summary>
	/// Supported records:
	/// User ID/Record ID
	/// LASF_Projection/2111    OGC MATH TRANSFORM WKT RECORD
	/// LASF_Projection/2112    OGC COORDINATE SYSTEM WKT
	/// LASF_Projection/34735   GeoKeyDirectoryTag
	/// LASF_Projection/34736   GeoDoubleParamsTag Record
	/// LASF_Projection/34737   GeoAsciiParamsTag Record
	/// LASF_Spec/0             Classification lookup
	/// LASF_Spec/3             Text area description
	/// LASF_Spec/4             Extra bytes
	/// LASF_Spec/7             Superseded
	/// LASF_Spec/n             Waveform packet descriptor
	///		where 99 < n < 355
	/// </summary>
	public class LASVLR : ISerializeBinary
	{
		private const ushort HeaderLength = 54;

		private static readonly Dictionary<LASRecordIdentifier, bool> c_knownRecordMapping;

		private readonly ushort m_reserved;
		private readonly string m_userID;
		private readonly ushort m_recordID;
		private readonly ushort m_recordLengthAfterHeader;
		private readonly string m_description;

		private readonly byte[] m_data;

		static LASVLR()
		{
			c_knownRecordMapping = new Dictionary<LASRecordIdentifier, bool>();
			c_knownRecordMapping.Add(new LASRecordIdentifier("LASF_Projection", 2111), true);
			c_knownRecordMapping.Add(new LASRecordIdentifier("LASF_Projection", 2112), true);
			c_knownRecordMapping.Add(new LASRecordIdentifier("LASF_Projection", 34735), true);
			c_knownRecordMapping.Add(new LASRecordIdentifier("LASF_Projection", 34736), true);
			c_knownRecordMapping.Add(new LASRecordIdentifier("LASF_Projection", 34737), true);
			c_knownRecordMapping.Add(new LASRecordIdentifier("LASF_Spec", 0), true);
			c_knownRecordMapping.Add(new LASRecordIdentifier("LASF_Spec", 3), true);
			c_knownRecordMapping.Add(new LASRecordIdentifier("LASF_Spec", 4), true);
			c_knownRecordMapping.Add(new LASRecordIdentifier("LASF_Spec", 7), false);
		}

		public static void AddInterestingRecord(LASRecordIdentifier recordIdentifier)
		{
			if (!c_knownRecordMapping.ContainsKey(recordIdentifier))
				c_knownRecordMapping.Add(recordIdentifier, true);
		}

		public static bool IsKnownRecord(LASRecordIdentifier recordIdentifier)
		{
			return c_knownRecordMapping.ContainsKey(recordIdentifier);
		}

		public static bool IsInterestingRecord(LASRecordIdentifier recordIdentifier)
		{
			bool value;
			if (c_knownRecordMapping.TryGetValue(recordIdentifier, out value))
				return value;

			return false;
		}

		public LASRecordIdentifier RecordIdentifier
		{
			get { return new LASRecordIdentifier(m_userID, m_recordID); }
		}

		public bool IsKnown
		{
			get { return IsKnownRecord(RecordIdentifier); }
		}

		public bool IsInteresting
		{
			get { return IsInterestingRecord(RecordIdentifier); }
		}

		public byte[] Data
		{
			get { return m_data; }
		}

		public uint Length
		{
			get { return (uint)(HeaderLength + m_recordLengthAfterHeader); }
		}

		public LASVLR(BinaryReader reader)
		{
			m_reserved = reader.ReadUInt16();
			m_userID = reader.ReadBytes(16).ToAsciiString();
			m_recordID = reader.ReadUInt16();
			m_recordLengthAfterHeader = reader.ReadUInt16();
			m_description = reader.ReadBytes(32).ToAsciiString();
			m_data = reader.ReadBytes(m_recordLengthAfterHeader);
		}

		public void Serialize(BinaryWriter writer)
		{
			writer.Write(m_reserved);
			writer.Write(m_userID.ToAsciiBytes(16));
			writer.Write(m_recordID);
			writer.Write(m_recordLengthAfterHeader);
			writer.Write(m_description.ToAsciiBytes(32));
			writer.Write(m_data);
		}

		public override string ToString()
		{
			return string.Format("{0} \"{1}\" {2} [{3}]", m_userID, m_description, m_recordID, m_recordLengthAfterHeader);
		}
	}
}
