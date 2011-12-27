using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CloudAE.Core.Compression
{
	public enum CompressionMethod : int
	{
		None = 0,
		Default,
		QuickLZ,
		DotNetZip,
		SevenZipSharp
	};
}
