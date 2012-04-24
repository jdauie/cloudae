using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CloudAE.Core
{
	public class PointCloudTileSourceEnumeratorChunk : IProgress
	{
		public readonly PointCloudTile Tile;

		public float Progress
		{
			get { return Tile.Progress; }
		}

		public PointCloudTileSourceEnumeratorChunk(PointCloudTile tile)
		{
			Tile = tile;
		}
	}
}
