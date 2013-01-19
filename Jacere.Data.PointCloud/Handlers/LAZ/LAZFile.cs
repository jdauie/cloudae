using System;
using System.Collections.Generic;
using System.Linq;

using Jacere.Core;

namespace Jacere.Data.PointCloud
{
	class LAZFile : LASFile
	{
		private static readonly LASRecordIdentifier c_record;
		private readonly LASVLR m_lazEncodedVLR;

		static LAZFile()
		{
			c_record = new LASRecordIdentifier("laszip encoded", 22204);
			LASVLR.AddInterestingRecord(c_record);
		}

		public LASVLR EncodedVLR
		{
			get { return m_lazEncodedVLR; }
		}

		public LAZFile(string path)
			: base(path)
		{
			m_lazEncodedVLR = m_vlrs.FirstOrDefault(vlr => vlr.RecordIdentifier.Equals(c_record));

			if (m_lazEncodedVLR == null)
				throw new Exception("no laz record");
		}

		public override IStreamReader GetStreamReader()
		{
			return new LAZStreamReader(FilePath, Header, m_lazEncodedVLR);
		}

		protected override PointCloudBinarySource CreateBinaryWrapper()
		{
			var source = new LAZBinarySource(this, Count, Extent, Header.Quantization, PointDataOffset, PointSizeBytes);

			return source;
		}
	}
}
