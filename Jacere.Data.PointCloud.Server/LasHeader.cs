using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace Jacere.Data.PointCloud.Server
{
    public enum LasVersion : ushort
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
    public class LasProjectId
    {
        private readonly byte[] _data;
        //private readonly Guid _guid;

        public LasProjectId()
        {
            _data = new byte[16];
        }

        public LasProjectId(BinaryReader reader)
        {
            _data = reader.ReadBytes(16);
            //_guid = new Guid(_data);
        }
    }

    public class LasVersionInfo
    {
        private readonly byte _versionMajor;
        private readonly byte _versionMinor;
        private readonly ushort _versionCombined;
        private readonly LasVersion _maxSupportedVersion;
        private readonly bool _isRecognizedVersion;

        public LasVersion Version => _maxSupportedVersion;

        public LasVersionInfo(BinaryReader reader)
        {
            _versionMajor = reader.ReadByte();
            _versionMinor = reader.ReadByte();

            _versionCombined = (ushort)((_versionMajor << 8) + _versionMinor);
            ushort[] versions = (ushort[])Enum.GetValues(typeof(LasVersion));

            int versionIndex = Array.IndexOf(versions, _versionCombined);

            if (versionIndex < 0)
            {
                // unknown version; may not be supported
                versionIndex = versions.Length - 1;
            }
            else
            {
                _isRecognizedVersion = true;
            }

            _maxSupportedVersion = (LasVersion)versions[versionIndex];
        }
    }

    public class LasGlobalEncoding
    {
        private readonly ushort _globalEncoding;

        public readonly bool AdjustedStandardGpsTime;
        public readonly bool WaveformDataPacketsExternal;
        public readonly bool ReturnNumbersSynthetic;
        public readonly bool Wkt;

        public LasGlobalEncoding()
        {
        }

        public LasGlobalEncoding(BinaryReader reader)
        {
            _globalEncoding = reader.ReadUInt16();

            AdjustedStandardGpsTime = (_globalEncoding & (1 << 0)) != 0;
            WaveformDataPacketsExternal = (_globalEncoding & (1 << 2)) != 0;
            ReturnNumbersSynthetic = (_globalEncoding & (1 << 3)) != 0;
            Wkt = (_globalEncoding & (1 << 4)) != 0;
        }
    }

    public class LasHeader
    {
        public const string FileSignature = "LASF";

        private static readonly Dictionary<LasVersion, ushort> MinHeaderSize;

        private readonly ushort _fileSourceId;

        private readonly LasGlobalEncoding _globalEncoding;
        private readonly LasProjectId _projectId;
        private readonly LasVersionInfo _version;

        private readonly string _systemIdentifier;
        private readonly string _generatingSoftware;
        private readonly ushort _fileCreationDayOfYear;
        private readonly ushort _fileCreationYear;

        private readonly ushort _headerSize;
        private readonly uint _offsetToPointData;

        private readonly uint _numberOfVariableLengthRecords;
        private readonly byte _pointDataRecordFormat;
        private readonly ushort _pointDataRecordLength;
        private readonly uint _legacyNumberOfPointRecords;
        private readonly uint[] _legacyNumberOfPointsByReturn;

        private readonly SQuantization3D _quantization;
        private readonly Extent3D _extent;

        // LAS 1.3
        private readonly ulong _startOfWaveformDataPacketRecord;

        // LAS 1.4
        private readonly ulong _startOfFirstExtendedVariableLengthRecord;
        private readonly uint _numberOfExtendedVariableLengthRecords;
        private readonly ulong _numberOfPointRecords;
        private readonly ulong[] _numberOfPointsByReturn;
        
        public ulong PointCount => _numberOfPointRecords;

        public SQuantization3D Quantization => _quantization;

        public Extent3D Extent => _extent;

        public uint OffsetToPointData => _offsetToPointData;

        public byte PointDataRecordFormat => _pointDataRecordFormat;

        public ushort PointDataRecordLength => _pointDataRecordLength;
        
        static LasHeader()
        {
            MinHeaderSize = new Dictionary<LasVersion, ushort>
            {
                {LasVersion.LAS_1_0, 227},
                {LasVersion.LAS_1_1, 227},
                {LasVersion.LAS_1_2, 227},
                {LasVersion.LAS_1_3, 235},
                {LasVersion.LAS_1_4, 375},
            };
        }
        
        public LasHeader(BinaryReader reader)
        {
            var length = reader.BaseStream.Length;

            if (length < MinHeaderSize[LasVersion.LAS_1_0])
                throw new Exception("Invalid format: header too short");

            if (Encoding.ASCII.GetString(reader.ReadBytes(FileSignature.Length)) != FileSignature)
                throw new Exception("Invalid format: signature does not match");

            _fileSourceId = reader.ReadUInt16();

            _globalEncoding = reader.ReadLasGlobalEncoding();
            _projectId = reader.ReadLasProjectId();
            _version = reader.ReadLasVersionInfo();

            _systemIdentifier = reader.ReadBytes(32).ToAsciiString();
            _generatingSoftware = reader.ReadBytes(32).ToAsciiString();
            _fileCreationDayOfYear = reader.ReadUInt16();
            _fileCreationYear = reader.ReadUInt16();

            _headerSize = reader.ReadUInt16();
            _offsetToPointData = reader.ReadUInt32();

            var minHeaderSize = MinHeaderSize[_version.Version];
            if (length < minHeaderSize)
                throw new Exception("Invalid format: header too short for version");
            if (minHeaderSize > _headerSize)
                throw new Exception("Invalid format: header size incorrect");

            _numberOfVariableLengthRecords = reader.ReadUInt32();
            _pointDataRecordFormat = reader.ReadByte();
            _pointDataRecordLength = reader.ReadUInt16();
            _legacyNumberOfPointRecords = reader.ReadUInt32();
            _legacyNumberOfPointsByReturn = reader.ReadUInt32Array(5);

            _quantization = reader.ReadSQuantization3D();
            _extent = reader.ReadExtent3D();

            if (_version.Version >= LasVersion.LAS_1_3)
            {
                _startOfWaveformDataPacketRecord = reader.ReadUInt64();
            }

            if (_version.Version >= LasVersion.LAS_1_4)
            {
                _startOfFirstExtendedVariableLengthRecord = reader.ReadUInt64();
                _numberOfExtendedVariableLengthRecords = reader.ReadUInt32();
                _numberOfPointRecords = reader.ReadUInt64();
                _numberOfPointsByReturn = reader.ReadUInt64Array(15);
            }
            else
            {
                _numberOfPointRecords = _legacyNumberOfPointRecords;
                _numberOfPointsByReturn = new ulong[15];
                for (var i = 0; i < _legacyNumberOfPointsByReturn.Length; i++)
                    _numberOfPointsByReturn[i] = _legacyNumberOfPointsByReturn[i];
            }

            // This doesn't apply to LAZ files
            //ulong pointDataRegionLength = (ulong)length - _offsetToPointData;
            //if (pointDataRegionLength < _pointDataRecordLength * PointCount)
            //    throw new Exception("Invalid format: point data region is not the expected size");
        }
        
        public LasVlr[] ReadVlrs(Stream stream)
        {
            var vlrs = new List<LasVlr>((int)_numberOfVariableLengthRecords);

            if (_numberOfVariableLengthRecords > 0)
            {
                stream.Seek(_headerSize, SeekOrigin.Begin);

                using (var reader = new BinaryReader(stream, Encoding.UTF8, true))
                {
                    for (var i = 0; i < _numberOfVariableLengthRecords; i++)
                    {
                        var vlr = reader.ReadObject<LasVlr>();
                        vlrs.Add(vlr);
                    }
                }
            }

            return vlrs.ToArray();
        }
        
        public LasEvlr[] ReadEvlrs(Stream stream)
        {
            var vlrs = new List<LasEvlr>((int)_numberOfExtendedVariableLengthRecords);

            if (_numberOfExtendedVariableLengthRecords > 0)
            {
                stream.Seek((long)_startOfFirstExtendedVariableLengthRecord, SeekOrigin.Begin);

                using (var reader = new BinaryReader(stream, Encoding.UTF8, true))
                {
                    for (var i = 0; i < _numberOfExtendedVariableLengthRecords; i++)
                    {
                        var vlr = reader.ReadObject<LasEvlr>();
                        if (vlr != null)
                            vlrs.Add(vlr);
                    }
                }
            }

            return vlrs.ToArray();
        }
    }
}
