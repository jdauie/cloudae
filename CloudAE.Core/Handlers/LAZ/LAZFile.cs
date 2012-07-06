using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;

using CloudAE.Interop.LAZ;

namespace CloudAE.Core
{
	class LAZFile : FileHandlerBase
	{
		public LAZFile(string path)
			: base(path)
		{
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
