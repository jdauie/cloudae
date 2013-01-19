using System;
using System.Collections.Generic;
using System.Linq;

namespace Jacere.Data.PointCloud
{
	public class LASRecordIdentifier : IEquatable<LASRecordIdentifier>
	{
		public readonly string UserID;
		public readonly ushort RecordID;

		public LASRecordIdentifier(string userID, ushort recordID)
		{
			UserID = userID;
			RecordID = recordID;
		}

		public override bool Equals(object obj)
		{
			var other = obj as LASRecordIdentifier;
			return (other != null && Equals(other));
		}

		public override int GetHashCode()
		{
			return RecordID;
		}

		#region IEquatable<LASRecordIdentifier> Members

		public bool Equals(LASRecordIdentifier other)
		{
			return (UserID == other.UserID && RecordID == other.RecordID);
		}

		#endregion
	}
}
