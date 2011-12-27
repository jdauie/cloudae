using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using CloudAE.Core.Geometry;

namespace CloudAE.Core
{
	public static class StreamExtensions
	{
		public static void Write(this BinaryWriter writer, ISerializeBinary obj)
		{
			obj.Serialize(writer);
		}

		public static Extent3D ReadExtent3D(this BinaryReader reader)
		{
			return new Extent3D(reader);
		}

		public static UQuantizedExtent3D ReadUQuantizedExtent3D(this BinaryReader reader)
		{
			return new UQuantizedExtent3D(reader);
		}

		public static PointCloudTileDensity ReadTileDensity(this BinaryReader reader)
		{
			return new PointCloudTileDensity(reader);
		}

		public static UQuantization3D ReadUQuantization3D(this BinaryReader reader)
		{
			return new UQuantization3D(reader);
		}

		public static PointCloudTileSet ReadTileSet(this BinaryReader reader)
		{
			return new PointCloudTileSet(reader);
		}

		public static Statistics ReadStatistics(this BinaryReader reader)
		{
			return new Statistics(reader);
		}
	}
}
