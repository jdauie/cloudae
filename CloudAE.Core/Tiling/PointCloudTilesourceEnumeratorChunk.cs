using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CloudAE.Core
{
	public class PointCloudTileSourceEnumeratorChunk
	{
		public readonly PointCloudTile Tile;
		public readonly float EnumeratorProgress;

		public PointCloudTileSourceEnumeratorChunk(PointCloudTile tile, float progress)
		{
			Tile = tile;
			EnumeratorProgress = progress;
		}
	}
}
