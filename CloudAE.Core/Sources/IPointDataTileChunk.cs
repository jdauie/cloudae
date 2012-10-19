using System;

namespace CloudAE.Core
{
	public interface IPointDataTileChunk : IPointDataProgressChunk
	{
		PointCloudTile Tile { get; }
	}
}
