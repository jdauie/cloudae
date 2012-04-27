using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Diagnostics;
using CloudAE.Core.Geometry;

namespace CloudAE.Core
{
	public enum LASVersion : ushort
	{
		LAS_1_0 = (1 << 8) | 0,
		LAS_1_1 = (1 << 8) | 1,
		LAS_1_2 = (1 << 8) | 2,
		LAS_1_3 = (1 << 8) | 3,
		LAS_1_4 = (1 << 8) | 4
	}

	/// <summary>
	/// Project ID replaces GUID data beginning in LAS 1.4
	/// </summary>
	public class LASProjectID : ISerializeBinary
	{
		private readonly byte[] m_data;
		//private readonly Guid m_guid;

		public LASProjectID(BinaryReader reader)
		{
			m_data = reader.ReadBytes(16);
			//m_guid = new Guid(m_data);
		}

		public void Serialize(BinaryWriter writer)
		{
			writer.Write(m_data);
		}
	}

	public class LASVersionInfo : ISerializeBinary
	{
		private readonly byte m_versionMajor;
		private readonly byte m_versionMinor;
		private readonly ushort m_versionCombined;
		private readonly LASVersion m_maxSupportedVersion;
		private readonly bool m_isRecognizedVersion;

		public LASVersion Version
		{
			get { return m_maxSupportedVersion; }
		}

		public LASVersionInfo(BinaryReader reader)
		{
			m_versionMajor = reader.ReadByte();
			m_versionMinor = reader.ReadByte();

			m_versionCombined = (ushort)((m_versionMajor << 8) + m_versionMinor);
			ushort[] versions = (ushort[])Enum.GetValues(typeof(LASVersion));

			int versionIndex = Array.IndexOf<ushort>(versions, m_versionCombined);

			if (versionIndex < 0)
			{
				// unknown version; may not be supported
				versionIndex = versions.Length - 1;
			}
			else
			{
				m_isRecognizedVersion = true;
			}

			m_maxSupportedVersion = (LASVersion)versions[versionIndex];
		}

		public void Serialize(BinaryWriter writer)
		{

		}
	}

	public class LASGlobalEncoding : ISerializeBinary
	{
		private readonly ushort m_globalEncoding;

		public readonly bool AdjustedStandardGPSTime;
		public readonly bool WaveformDataPacketsExternal;
		public readonly bool ReturnNumbersSynthetic;
		public readonly bool WKT;

		public LASGlobalEncoding(BinaryReader reader)
		{
			m_globalEncoding = reader.ReadUInt16();

			AdjustedStandardGPSTime     = (m_globalEncoding & (1 << 0)) != 0;
			WaveformDataPacketsExternal = (m_globalEncoding & (1 << 2)) != 0;
			ReturnNumbersSynthetic      = (m_globalEncoding & (1 << 3)) != 0;
			WKT                         = (m_globalEncoding & (1 << 4)) != 0;
		}

		public void Serialize(BinaryWriter writer)
		{
			writer.Write(m_globalEncoding);
		}
	}

	public class LASHeader : ISerializeBinary
	{
		public const string FILE_SIGNATURE = "LASF";

		private static readonly Dictionary<LASVersion, ushort> c_minHeaderSize;

		private readonly ushort hFileSourceID;

		private readonly LASGlobalEncoding m_globalEncoding;
		private readonly LASProjectID m_projectID;
		private readonly LASVersionInfo m_version;

		private readonly string hSystemIdentifier;
		private readonly string hGeneratingSoftware;
		private readonly ushort hFileCreationDayOfYear;
		private readonly ushort hFileCreationYear;

		private readonly ushort hHeaderSize;
		private readonly uint hOffsetToPointData;

		private readonly uint hNumberOfVariableLengthRecords;
		private readonly byte hPointDataRecordFormat;
		private readonly ushort hPointDataRecordLength;
		private readonly uint hLegacyNumberOfPointRecords;
		private readonly uint[] hLegacyNumberOfPointsByReturn;

		private readonly SQuantization3D m_quantization;
		private readonly Extent3D m_extent;

		// LAS 1.3
		private readonly ulong hStartOfWaveformDataPacketRecord;

		// LAS 1.4
		private readonly ulong hStartOfFirstExtendedVariableLengthRecord;
		private readonly uint hNumberOfExtendedVariableLengthRecords;
		private readonly ulong hNumberOfPointRecords;
		private readonly ulong[] hNumberOfPointsByReturn;

		#region Properties

		public ulong PointCount
		{
			get { return hNumberOfPointRecords; }
		}

		public SQuantization3D Quantization
		{
			get { return m_quantization; }
		}

		public Extent3D Extent
		{
			get { return m_extent; }
		}

		public uint OffsetToPointData
		{
			get { return hOffsetToPointData; }
		}

		public ushort PointDataRecordLength
		{
			get { return hPointDataRecordLength; }
		}

		#endregion

		static LASHeader()
		{
			c_minHeaderSize = new Dictionary<LASVersion,ushort>();
			c_minHeaderSize.Add(LASVersion.LAS_1_0, 219);
			c_minHeaderSize.Add(LASVersion.LAS_1_1, 219);
			c_minHeaderSize.Add(LASVersion.LAS_1_2, 219);
			c_minHeaderSize.Add(LASVersion.LAS_1_3, 227);
			c_minHeaderSize.Add(LASVersion.LAS_1_4, 267);
		}

		public LASHeader(BinaryReader reader)
		{
			long length = reader.BaseStream.Length;

			if (length < c_minHeaderSize[LASVersion.LAS_1_0])
				throw new Exception("Invalid format: header too short");

			if (ASCIIEncoding.ASCII.GetString(reader.ReadBytes(FILE_SIGNATURE.Length)) != FILE_SIGNATURE)
				throw new Exception("Invalid format: signature does not match");

			hFileSourceID = reader.ReadUInt16();

			m_globalEncoding = reader.ReadLASGlobalEncoding();
			m_projectID = reader.ReadLASProjectID();
			m_version = reader.ReadLASVersionInfo();
			
			hSystemIdentifier = reader.ReadBytes(32).UnsafeAsciiBytesToString();
			hGeneratingSoftware = reader.ReadBytes(32).UnsafeAsciiBytesToString();
			hFileCreationDayOfYear = reader.ReadUInt16();
			hFileCreationYear = reader.ReadUInt16();

			hHeaderSize = reader.ReadUInt16();
			hOffsetToPointData = reader.ReadUInt32();

			ushort minHeaderSize = c_minHeaderSize[m_version.Version];
			if (length < minHeaderSize)
				throw new Exception("Invalid format: header too short for version");
			if(minHeaderSize > hHeaderSize)
				throw new Exception("Invalid format: header size incorrect");

			hNumberOfVariableLengthRecords = reader.ReadUInt32();
			hPointDataRecordFormat = reader.ReadByte();
			hPointDataRecordLength = reader.ReadUInt16();
			hLegacyNumberOfPointRecords = reader.ReadUInt32();
			hLegacyNumberOfPointsByReturn = reader.ReadUInt32Array(5);

			m_quantization = reader.ReadSQuantization3D();
			m_extent = reader.ReadLASExtent3D();

			if (m_version.Version >= LASVersion.LAS_1_3)
			{
				hStartOfWaveformDataPacketRecord = reader.ReadUInt64();
			}

			if (m_version.Version >= LASVersion.LAS_1_4)
			{
				hStartOfFirstExtendedVariableLengthRecord = reader.ReadUInt64();
				hNumberOfExtendedVariableLengthRecords = reader.ReadUInt32();
				hNumberOfPointRecords = reader.ReadUInt64();
				hNumberOfPointsByReturn = reader.ReadUInt64Array(15);
			}
			else
			{
				hNumberOfPointRecords = hLegacyNumberOfPointRecords;
				hNumberOfPointsByReturn = new ulong[15];
				for (int i = 0; i < hLegacyNumberOfPointsByReturn.Length; i++)
					hNumberOfPointsByReturn[i] = hLegacyNumberOfPointsByReturn[i];
			}

			ulong pointDataRegionLength = (ulong)length - hOffsetToPointData;
			if (pointDataRegionLength < hPointDataRecordLength * PointCount)
				throw new Exception("Invalid format: point data region is not the expected size");
		}

		public void Serialize(BinaryWriter writer)
		{
			writer.Write(ASCIIEncoding.ASCII.GetBytes(FILE_SIGNATURE));
			writer.Write(hFileSourceID);
			writer.Write(m_globalEncoding);
			writer.Write(m_projectID);
			writer.Write(m_version);

			writer.Write(hSystemIdentifier.ToUnsafeAsciiBytes(32));
			writer.Write(hGeneratingSoftware.ToUnsafeAsciiBytes(32));
			writer.Write(hFileCreationDayOfYear);
			writer.Write(hFileCreationYear);
			writer.Write(hHeaderSize);
			writer.Write(hOffsetToPointData);

			writer.Write(hNumberOfVariableLengthRecords);
			writer.Write(hPointDataRecordFormat);
			writer.Write(hPointDataRecordLength);
			writer.Write(hLegacyNumberOfPointRecords);
			writer.Write(hLegacyNumberOfPointsByReturn);
			writer.Write(m_quantization);
			writer.Write(m_extent);

			if (m_version.Version >= LASVersion.LAS_1_3)
			{
				writer.Write(hStartOfFirstExtendedVariableLengthRecord);
			}

			if (m_version.Version >= LASVersion.LAS_1_4)
			{
				writer.Write(hStartOfFirstExtendedVariableLengthRecord);
				writer.Write(hNumberOfExtendedVariableLengthRecords);
				writer.Write(hNumberOfPointRecords);
				writer.Write(hNumberOfPointsByReturn);
			}
		}
	}
}
