using System;

using Jacere.Data.PointCloud;

namespace CloudAE.Core
{
	public interface IPointDataTileChunk : IPointDataProgressChunk
	{
		PointCloudTile Tile { get; }
	}
}
