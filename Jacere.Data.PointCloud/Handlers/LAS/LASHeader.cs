using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Diagnostics;
using Jacere.Core;
using Jacere.Data.PointCloud.Geometry;

namespace Jacere.Data.PointCloud
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

			int versionIndex = Array.IndexOf(versions, m_versionCombined);

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

		private LASVersionInfo(LASVersion version)
		{
			m_versionCombined = (ushort)version;
			m_versionMajor = (byte)(m_versionCombined >> 8);
			m_versionMinor = (byte)(m_versionCombined & byte.MaxValue);
		}

		public void Serialize(BinaryWriter writer)
		{
			writer.Write(m_versionMajor);
			writer.Write(m_versionMinor);
		}

		public static LASVersionInfo Create(LASVersion version)
		{
			var versionInfo = new LASVersionInfo(version);
			return (LASVersionInfo)SerializationHelper.Clone(versionInfo);
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

		private readonly ushort m_fileSourceID;

		private readonly LASGlobalEncoding m_globalEncoding;
		private readonly LASProjectID m_projectID;
		private readonly LASVersionInfo m_version;

		private readonly string m_systemIdentifier;
		private readonly string m_generatingSoftware;
		private readonly ushort m_fileCreationDayOfYear;
		private readonly ushort m_fileCreationYear;

		private readonly ushort m_headerSize;
		private readonly uint m_offsetToPointData;

		private readonly uint m_numberOfVariableLengthRecords;
		private readonly byte m_pointDataRecordFormat;
		private readonly ushort m_pointDataRecordLength;
		private readonly uint m_legacyNumberOfPointRecords;
		private readonly uint[] m_legacyNumberOfPointsByReturn;

		private readonly SQuantization3D m_quantization;
		private readonly Extent3D m_extent;

		// LAS 1.3
		private readonly ulong m_startOfWaveformDataPacketRecord;

		// LAS 1.4
		private readonly ulong m_startOfFirstExtendedVariableLengthRecord;
		private readonly uint m_numberOfExtendedVariableLengthRecords;
		private readonly ulong m_numberOfPointRecords;
		private readonly ulong[] m_numberOfPointsByReturn;

		#region Properties

		public ulong PointCount
		{
			get { return m_numberOfPointRecords; }
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
			get { return m_offsetToPointData; }
		}

		public ushort PointDataRecordLength
		{
			get { return m_pointDataRecordLength; }
		}

		#endregion

		static LASHeader()
		{
			c_minHeaderSize = new Dictionary<LASVersion,ushort>();
			c_minHeaderSize.Add(LASVersion.LAS_1_0, 227);
			c_minHeaderSize.Add(LASVersion.LAS_1_1, 227);
			c_minHeaderSize.Add(LASVersion.LAS_1_2, 227);
			c_minHeaderSize.Add(LASVersion.LAS_1_3, 235);
			c_minHeaderSize.Add(LASVersion.LAS_1_4, 375);
		}

		public LASHeader(BinaryReader reader)
		{
			long length = reader.BaseStream.Length;

			if (length < c_minHeaderSize[LASVersion.LAS_1_0])
				throw new OpenFailedException("Invalid format: header too short");

			if (Encoding.ASCII.GetString(reader.ReadBytes(FILE_SIGNATURE.Length)) != FILE_SIGNATURE)
				throw new OpenFailedException("Invalid format: signature does not match");

			m_fileSourceID          = reader.ReadUInt16();

			m_globalEncoding        = reader.ReadLASGlobalEncoding();
			m_projectID             = reader.ReadLASProjectID();
			m_version               = reader.ReadLASVersionInfo();
			
			m_systemIdentifier      = reader.ReadBytes(32).ToAsciiString();
			m_generatingSoftware    = reader.ReadBytes(32).ToAsciiString();
			m_fileCreationDayOfYear = reader.ReadUInt16();
			m_fileCreationYear      = reader.ReadUInt16();

			m_headerSize            = reader.ReadUInt16();
			m_offsetToPointData     = reader.ReadUInt32();

			ushort minHeaderSize = c_minHeaderSize[m_version.Version];
			if (length < minHeaderSize)
				throw new OpenFailedException("Invalid format: header too short for version");
			if(minHeaderSize > m_headerSize)
				throw new OpenFailedException("Invalid format: header size incorrect");

			m_numberOfVariableLengthRecords = reader.ReadUInt32();
			m_pointDataRecordFormat         = reader.ReadByte();
			m_pointDataRecordLength         = reader.ReadUInt16();
			m_legacyNumberOfPointRecords    = reader.ReadUInt32();
			m_legacyNumberOfPointsByReturn  = reader.ReadUInt32Array(5);

			m_quantization = reader.ReadSQuantization3D();
			m_extent       = reader.ReadExtent3D();

			if (m_version.Version >= LASVersion.LAS_1_3)
			{
				m_startOfWaveformDataPacketRecord = reader.ReadUInt64();
			}

			if (m_version.Version >= LASVersion.LAS_1_4)
			{
				m_startOfFirstExtendedVariableLengthRecord = reader.ReadUInt64();
				m_numberOfExtendedVariableLengthRecords    = reader.ReadUInt32();
				m_numberOfPointRecords                     = reader.ReadUInt64();
				m_numberOfPointsByReturn                   = reader.ReadUInt64Array(15);
			}
			else
			{
				m_numberOfPointRecords = m_legacyNumberOfPointRecords;
				m_numberOfPointsByReturn = new ulong[15];
				for (int i = 0; i < m_legacyNumberOfPointsByReturn.Length; i++)
					m_numberOfPointsByReturn[i] = m_legacyNumberOfPointsByReturn[i];
			}

			// This doesn't apply to LAZ files
			//ulong pointDataRegionLength = (ulong)length - m_offsetToPointData;
			//if (pointDataRegionLength < m_pointDataRecordLength * PointCount)
			//    throw new Exception("Invalid format: point data region is not the expected size");
		}

		public LASHeader(LASHeader[] headers, PointCloudTileSource source)
		{
			//LASHeader header = headers[0];

			//m_fileSourceID = header.m_fileSourceID;

			//m_globalEncoding = header.m_globalEncoding;
			//m_projectID = header.m_projectID;
			//m_version = LASVersionInfo.Create(LASVersion.LAS_1_4);

			//m_systemIdentifier = header.m_systemIdentifier;
			//m_generatingSoftware = header.m_generatingSoftware;
			//m_fileCreationDayOfYear = header.m_fileCreationDayOfYear;
			//m_fileCreationYear = header.m_fileCreationYear;

			//m_headerSize = c_minHeaderSize[m_version.Version];
			//m_offsetToPointData = reader.ReadUInt32();

			//m_numberOfVariableLengthRecords = 0;
			//m_pointDataRecordFormat = header.m_pointDataRecordFormat;
			//m_pointDataRecordLength = header.m_pointDataRecordLength;
			//m_legacyNumberOfPointRecords = header.m_legacyNumberOfPointRecords;
			//m_legacyNumberOfPointsByReturn = header.m_legacyNumberOfPointsByReturn;

			//m_quantization = quantization;
			//m_extent = extent;

			//if (m_version.Version >= LASVersion.LAS_1_4)
			//{
			//    m_startOfFirstExtendedVariableLengthRecord = reader.ReadUInt64();
			//    m_numberOfExtendedVariableLengthRecords = reader.ReadUInt32();
			//    m_numberOfPointRecords = reader.ReadUInt64();
			//    m_numberOfPointsByReturn = reader.ReadUInt64Array(15);
			//}
			//else
			//{
			//    m_numberOfPointRecords = m_legacyNumberOfPointRecords;
			//    m_numberOfPointsByReturn = new ulong[15];
			//    for (int i = 0; i < m_legacyNumberOfPointsByReturn.Length; i++)
			//        m_numberOfPointsByReturn[i] = m_legacyNumberOfPointsByReturn[i];
			//}
		}

		public void Serialize(BinaryWriter writer)
		{
			writer.Write(Encoding.ASCII.GetBytes(FILE_SIGNATURE));
			writer.Write(m_fileSourceID);
			writer.Write(m_globalEncoding);
			writer.Write(m_projectID);
			writer.Write(m_version);

			writer.Write(m_systemIdentifier.ToAsciiBytes(32));
			writer.Write(m_generatingSoftware.ToAsciiBytes(32));
			writer.Write(m_fileCreationDayOfYear);
			writer.Write(m_fileCreationYear);
			writer.Write(m_headerSize);
			writer.Write(m_offsetToPointData);

			writer.Write(m_numberOfVariableLengthRecords);
			writer.Write(m_pointDataRecordFormat);
			writer.Write(m_pointDataRecordLength);
			writer.Write(m_legacyNumberOfPointRecords);
			writer.Write(m_legacyNumberOfPointsByReturn);
			writer.Write(m_quantization);
			writer.Write(m_extent);

			if (m_version.Version >= LASVersion.LAS_1_3)
			{
				writer.Write(m_startOfWaveformDataPacketRecord);
			}

			if (m_version.Version >= LASVersion.LAS_1_4)
			{
				writer.Write(m_startOfFirstExtendedVariableLengthRecord);
				writer.Write(m_numberOfExtendedVariableLengthRecords);
				writer.Write(m_numberOfPointRecords);
				writer.Write(m_numberOfPointsByReturn);
			}
		}

		public bool IsCompatible(LASHeader other)
		{
			return (
				m_quantization == other.m_quantization &&
				m_pointDataRecordFormat == other.m_pointDataRecordFormat &&
				m_pointDataRecordLength == other.m_pointDataRecordLength
			);
		}

		public LASVLR[] ReadVLRs(Stream stream)
		{
			return ReadVLRs(stream, null);
		}

		public LASVLR[] ReadVLRs(Stream stream, Func<LASVLR, bool> acceptanceCondition)
		{
			var vlrs = new List<LASVLR>((int)m_numberOfVariableLengthRecords);

			if (m_numberOfVariableLengthRecords > 0)
			{
				stream.Seek(m_headerSize, SeekOrigin.Begin);

				using (var reader = new FlexibleBinaryReader(stream))
				{
					for (int i = 0; i < m_numberOfVariableLengthRecords; i++)
					{
						var vlr = reader.ReadObject(typeof(LASVLR)) as LASVLR;
						if (acceptanceCondition != null)
						{
							if (acceptanceCondition(vlr))
								vlrs.Add(vlr);
						}
						else
						{
							if (vlr.IsInteresting)
								vlrs.Add(vlr);
						}
					}
				}
			}

			return vlrs.ToArray();
		}

		public void WriteVLRs(Stream stream, LASVLR[] vlrs)
		{
			//m_numberOfVariableLengthRecords = vlrs.Length;

			// my stream doesn't seek for now
			stream.Seek(m_headerSize, SeekOrigin.Begin);

			// make a non-closing writer?
			using (var writer = new BinaryWriter(stream))
			{
				foreach (var vlr in vlrs)
				{
					writer.Write(vlr);
				}
			}
		}

		public void WriteEVLRs(Stream stream, LASEVLR[] vlrs)
		{
			
		}

		public LASEVLR[] ReadEVLRs(Stream stream)
		{
			var vlrs = new List<LASEVLR>((int)m_numberOfExtendedVariableLengthRecords);

			if (m_numberOfExtendedVariableLengthRecords > 0)
			{
				stream.Seek((long)m_startOfFirstExtendedVariableLengthRecord, SeekOrigin.Begin);

				using (var reader = new FlexibleBinaryReader(stream))
				{
					for (int i = 0; i < m_numberOfExtendedVariableLengthRecords; i++)
					{
						var vlr = reader.ReadObject(typeof(LASEVLR)) as LASEVLR;
						if (vlr != null && vlr.IsInteresting)
							vlrs.Add(vlr);
					}
				}
			}

			return vlrs.ToArray();
		}
	}
}
