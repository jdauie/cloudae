using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CloudAE.Core.Geometry;
using System.IO;

namespace CloudAE.Core
{
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
