using System;
using System.IO;

namespace CloudAE.Core
{
	public class PointCloudTileBufferManagerOptions
	{
		public readonly FileOptions TilingFileOptions;
		public readonly bool AllowSparseAllocation;
		public readonly bool SupportsSegmentedProcessing;

		public PointCloudTileBufferManagerOptions()
		{
			TilingFileOptions = FileOptions.RandomAccess | FileOptions.WriteThrough;
			AllowSparseAllocation = true;
			SupportsSegmentedProcessing = true;
		}

		public IPointCloudTileBufferManager CreateManager(PointCloudTileSource tileSource, FileStream outputStream)
		{
			return new PointCloudTileBufferManager2(tileSource, outputStream);
		}
	}
}
