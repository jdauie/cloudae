using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using Jacere.Core.Geometry;

namespace CloudAE.Core
{
	public static class StreamExtensions
	{
		public static PointCloudTileSet ReadTileSet(this BinaryReader reader)
		{
			return new PointCloudTileSet(reader);
		}

		public static PointCloudTileDensity ReadTileDensity(this BinaryReader reader)
		{
			return new PointCloudTileDensity(reader);
		}

		public static Statistics ReadStatistics(this BinaryReader reader)
		{
			return new Statistics(reader);
		}
	}
}
