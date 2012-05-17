using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CloudAE.Core.Geometry;
using System.IO;

namespace CloudAE.Core
{
	public enum PointCloudTileBufferManagerMode : ushort
	{
		None,
		OptimizeForLargeFile
	}

	public class PointCloudTileBufferManagerOptions
	{
		public readonly PointCloudTileBufferManagerMode Mode;
		public readonly FileOptions TilingFileOptions;
		public readonly bool AllowSparseAllocation;
		public readonly bool SupportsSegmentedProcessing;

		public PointCloudTileBufferManagerOptions(PointCloudTileBufferManagerMode mode)
		{
			Mode = mode;

			if (Mode == PointCloudTileBufferManagerMode.OptimizeForLargeFile)
			{
				TilingFileOptions = FileOptions.RandomAccess | FileOptions.WriteThrough;
				AllowSparseAllocation = true;
				SupportsSegmentedProcessing = true;
			}
			else
			{
				TilingFileOptions = FileOptions.RandomAccess | FileOptions.WriteThrough;
				AllowSparseAllocation = false;
				SupportsSegmentedProcessing = false;
			}
		}

		public IPointCloudTileBufferManager CreateManager(PointCloudTileSource tileSource, FileStream outputStream)
		{
			IPointCloudTileBufferManager tileBufferManager;

			if (Mode == PointCloudTileBufferManagerMode.OptimizeForLargeFile)
				tileBufferManager = new PointCloudTileBufferManager2(tileSource, outputStream);
			else
				tileBufferManager = new PointCloudTileBufferManager(tileSource, outputStream);

			return tileBufferManager;
		}
	}
}
