using System;

namespace Jacere.Data.PointCloud.Server
{
    public class LasRecordIdentifier : IEquatable<LasRecordIdentifier>
    {
        public readonly string UserId;
        public readonly ushort RecordId;

        public LasRecordIdentifier(string userId, ushort recordId)
        {
            UserId = userId;
            RecordId = recordId;
        }

        public override bool Equals(object obj)
        {
            var other = obj as LasRecordIdentifier;
            return other != null && Equals(other);
        }

        public override int GetHashCode()
        {
            return RecordId;
        }
        
        public bool Equals(LasRecordIdentifier other)
        {
            return UserId == other.UserId && RecordId == other.RecordId;
        }
    }
}
