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

        [Obsolete("Moving back to LAS compatibility", true)]
        public static UQuantizedExtent3D ReadUQuantizedExtent3D(this BinaryReader reader)
        {
            return new UQuantizedExtent3D(reader);
        }

        [Obsolete("Moving back to LAS compatibility", true)]
        public static UQuantizedPoint3D ReadUQuantizedPoint3D(this BinaryReader reader)
        {
            return new UQuantizedPoint3D(reader);
        }

		public static SQuantizedExtent3D ReadSQuantizedExtent3D(this BinaryReader reader)
		{
			return new SQuantizedExtent3D(reader);
		}

		public static SQuantizedPoint3D ReadSQuantizedPoint3D(this BinaryReader reader)
		{
			return new SQuantizedPoint3D(reader);
		}

        [Obsolete("Moving back to LAS compatibility", true)]
        public static UQuantization3D ReadUQuantization3D(this BinaryReader reader)
        {
            return new UQuantization3D(reader);
        }

		public static SQuantization3D ReadSQuantization3D(this BinaryReader reader)
		{
			return new SQuantization3D(reader);
		}
	}
}
