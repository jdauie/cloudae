using System;

namespace Jacere.Data.PointCloud
{
	public interface IChunkProcess
	{
		IPointDataChunk Process(IPointDataChunk chunk);
	}
}
