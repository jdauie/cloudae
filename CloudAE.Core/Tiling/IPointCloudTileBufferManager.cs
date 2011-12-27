using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CloudAE.Core.Geometry;
using System.IO;

namespace CloudAE.Core
{
	public enum PointCloudTileBufferManagerMode
	{
		None,
		OptimizeForLargeFile
	}

	public class PointCloudTileBufferManagerOptions
	{
		public readonly PointCloudTileBufferManagerMode Mode;
		public readonly FileOptions TilingFileOptions;
		public readonly bool AllowSparseAllocation;

		public PointCloudTileBufferManagerOptions(PointCloudTileBufferManagerMode mode)
		{
			Mode = mode;

			if (Mode == PointCloudTileBufferManagerMode.OptimizeForLargeFile)
			{
				TilingFileOptions = FileOptions.SequentialScan | FileOptions.WriteThrough;
				AllowSparseAllocation = true;
			}
			else
			{
				TilingFileOptions = FileOptions.RandomAccess | FileOptions.WriteThrough;
				AllowSparseAllocation = false;
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

	public interface IPointCloudTileBufferManager
	{
		/// <summary>
		/// Gets the tile source.
		/// </summary>
		/// <value>The tile source.</value>
		PointCloudTileSource TileSource
		{
			get;
		}

		/// <summary>
		/// Adds the point to the tiling, but provides no guarantee
		/// about when the point will be written to disk.
		/// </summary>
		/// <param name="point">The point.</param>
		/// <param name="tileX">The tile X.</param>
		/// <param name="tileY">The tile Y.</param>
		void AddPoint(UQuantizedPoint3D point, int tileX, int tileY);
		
		/// <summary>
		/// Finalizes the tiles.
		/// The progress manager is provided in case the operation hits the disk.
		/// </summary>
		/// <param name="progressManager">The progress manager.</param>
		UQuantizedExtent3D FinalizeTiles(ProgressManager progressManager);
	}
}
