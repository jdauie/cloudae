using System;

namespace CloudAE.Core
{
	public interface IPointDataTileChunk : IPointDataChunk
	{
		PointCloudTile Tile { get; }
	}
}
