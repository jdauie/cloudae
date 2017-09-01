using System.Collections.Generic;
using System.IO;

namespace Jacere.Data.PointCloud.Server
{
    /// <summary>
    /// Additional supported records:
    /// User ID/Record ID
    /// LASF_Spec/65535   Waveform data packets
    /// </summary>
    public class LasEvlr
    {
        private static readonly Dictionary<LasRecordIdentifier, bool> KnownRecordMapping;

        private readonly ushort _reserved;
        private readonly string _userId;
        private readonly ushort _recordId;
        private readonly ulong _recordLengthAfterHeader;
        private readonly string _description;

        private readonly byte[] _data;

        static LasEvlr()
        {
            KnownRecordMapping =
                new Dictionary<LasRecordIdentifier, bool>
                {
                    {new LasRecordIdentifier("LASF_Spec", 65535), false},
                };
        }

        public LasRecordIdentifier RecordIdentifier => new LasRecordIdentifier(_userId, _recordId);

        public bool IsKnown
        {
            get
            {
                var record = RecordIdentifier;
                return LasVlr.IsKnownRecord(record) || KnownRecordMapping.ContainsKey(record);
            }
        }

        public bool IsInteresting
        {
            get
            {
                var record = RecordIdentifier;
                var value = LasVlr.IsInterestingRecord(record);

                if (!value)
                {
                    if (KnownRecordMapping.TryGetValue(record, out value))
                        return value;
                }

                return value;
            }
        }
        
        public LasEvlr(BinaryReader reader)
        {
            _reserved = reader.ReadUInt16();
            _userId = reader.ReadBytes(16).ToAsciiString();
            _recordId = reader.ReadUInt16();
            _recordLengthAfterHeader = reader.ReadUInt64();
            _description = reader.ReadBytes(32).ToAsciiString();

            // this data could be massive...such as the waveform data packets
            // I should only read records that I want
            // If I later decide that I want to read large records, they should be streamed
            //reader.BaseStream.Seek((long)_recordLengthAfterHeader, SeekOrigin.Current);
            _data = reader.ReadBytes((int)_recordLengthAfterHeader);
        }
        
        public override string ToString()
        {
            return $"{_userId} \"{_description}\" {_recordId} [{_recordLengthAfterHeader}]";
        }
    }
}
