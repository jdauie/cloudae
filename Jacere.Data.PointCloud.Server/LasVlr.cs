using System.Collections.Generic;
using System.IO;

namespace Jacere.Data.PointCloud.Server
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
    /// LASF_Spec/n             Waveform packet descriptor (where n greater than 99 and less than 355)
    /// </summary>
    public class LasVlr
    {
        private const ushort HeaderLength = 54;

        private static readonly Dictionary<LasRecordIdentifier, bool> KnownRecordMapping;

        private readonly ushort _reserved;
        private readonly string _userId;
        private readonly ushort _recordId;
        private readonly ushort _recordLengthAfterHeader;
        private readonly string _description;

        private readonly byte[] _data;

        static LasVlr()
        {
            KnownRecordMapping = new Dictionary<LasRecordIdentifier, bool>
            {
                {new LasRecordIdentifier("LASF_Projection", 2111), true},
                {new LasRecordIdentifier("LASF_Projection", 2112), true},
                {new LasRecordIdentifier("LASF_Projection", 34735), true},
                {new LasRecordIdentifier("LASF_Projection", 34736), true},
                {new LasRecordIdentifier("LASF_Projection", 34737), true},
                {new LasRecordIdentifier("LASF_Spec", 0), true},
                {new LasRecordIdentifier("LASF_Spec", 3), true},
                {new LasRecordIdentifier("LASF_Spec", 4), true},
                {new LasRecordIdentifier("LASF_Spec", 7), false}
            };
        }

        public static void AddInterestingRecord(LasRecordIdentifier recordIdentifier)
        {
            if (!KnownRecordMapping.ContainsKey(recordIdentifier))
                KnownRecordMapping.Add(recordIdentifier, true);
        }

        public static bool IsKnownRecord(LasRecordIdentifier recordIdentifier)
        {
            return KnownRecordMapping.ContainsKey(recordIdentifier);
        }

        public static bool IsInterestingRecord(LasRecordIdentifier recordIdentifier)
        {
            bool value;
            if (KnownRecordMapping.TryGetValue(recordIdentifier, out value))
                return value;

            return false;
        }

        public LasRecordIdentifier RecordIdentifier => new LasRecordIdentifier(_userId, _recordId);

        public bool IsKnown => IsKnownRecord(RecordIdentifier);

        public bool IsInteresting => IsInterestingRecord(RecordIdentifier);

        public byte[] Data => _data;

        public uint Length => (uint)(HeaderLength + _recordLengthAfterHeader);

        public LasVlr(BinaryReader reader)
        {
            _reserved = reader.ReadUInt16();
            _userId = reader.ReadBytes(16).ToAsciiString();
            _recordId = reader.ReadUInt16();
            _recordLengthAfterHeader = reader.ReadUInt16();
            _description = reader.ReadBytes(32).ToAsciiString();
            _data = reader.ReadBytes(_recordLengthAfterHeader);
        }
        
        public override string ToString()
        {
            return $"{_userId} \"{_description}\" {_recordId} [{_recordLengthAfterHeader}]";
        }
    }
}
