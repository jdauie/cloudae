using System;

namespace Jacere.Data.PointCloud
{
	public interface IPointDataTileChunk : IPointDataProgressChunk
	{
		PointCloudTile Tile { get; }
	}
}
