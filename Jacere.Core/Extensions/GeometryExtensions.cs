using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;

using Jacere.Core.Geometry;

namespace Jacere.Core
{
	public static class GeometryExtensions
	{
		public static Point3D ReadPoint3D(this BinaryReader reader)
		{
			return new Point3D(reader);
		}

		public static Extent3D ReadExtent3D(this BinaryReader reader)
		{
			return new Extent3D(reader);
		}

		public static SQuantizedExtent3D ReadSQuantizedExtent3D(this BinaryReader reader)
		{
			return new SQuantizedExtent3D(reader);
		}

		public static SQuantizedPoint3D ReadSQuantizedPoint3D(this BinaryReader reader)
		{
			return new SQuantizedPoint3D(reader);
		}

		public static SQuantization3D ReadSQuantization3D(this BinaryReader reader)
		{
			return new SQuantization3D(reader);
		}
	}
}
