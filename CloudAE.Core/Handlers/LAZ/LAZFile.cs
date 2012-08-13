﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;

using CloudAE.Interop.LAZ;

namespace CloudAE.Core
{
	class LAZFile : FileHandlerBase
	{
		private readonly LASHeader m_header;
		private readonly LASVLR m_lazEncodedVLR;

		public LAZFile(string path)
			: base(path)
		{
			var record = new LASRecordIdentifier("laszip encoded", 22204);

			using (var stream = StreamManager.OpenReadStream(FilePath))
			{
				using (var reader = new FlexibleBinaryReader(stream))
				{
					m_header = reader.ReadLASHeader();
				}

				LASVLR[] vlrs = m_header.ReadVLRs(stream, r => r.RecordIdentifier.Equals(record));
				m_lazEncodedVLR = vlrs[0];
			}
		}

		public override IPointCloudBinarySource GenerateBinarySource(ProgressManager progressManager)
		{
			LAZInterop laz = new LAZInterop(FilePath, m_header.OffsetToPointData, m_lazEncodedVLR.Data);

			//using (var stream = StreamManager.OpenWriteStream("c:\\test.las", 0, 0))
			//{
			//    using (var writer = new BinaryWriter(stream))
			//    {
			//        m_header.Serialize(writer);
			//    }

			//    LASVLR[] vlrs = m_header.ReadVLRs(stream, r => r.RecordIdentifier.Equals(record));
			//    m_lazEncodedVLR = vlrs[0];
			//}

			//using (var stream = StreamManager.OpenWriteStream("c:\\test.las", 0, 0))
			//{
				
			//}

			throw new NotImplementedException("");
		}

		public override string GetPreview()
		{
			return "LAZ";
		}
	}
}
