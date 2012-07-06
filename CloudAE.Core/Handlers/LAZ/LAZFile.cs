using System;
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
				using (var reader = new FlexibleBinaryReader(stream, false))
				{
					m_header = reader.ReadLASHeader();
				}

				LASVLR[] vlrs = m_header.ReadVLRs(stream, r => r.RecordIdentifier.Equals(record));
				m_lazEncodedVLR = vlrs[0];
			}
		}

		public override PointCloudBinarySource GenerateBinarySource(ProgressManager progressManager)
		{
			LAZInterop laz = new LAZInterop();
			laz.unzip(FilePath);

			throw new NotImplementedException("");
		}

		public override string GetPreview()
		{
			return "LAZ";
		}
	}
}
