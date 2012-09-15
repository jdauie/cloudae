using System;
using System.Linq;
using System.IO;

namespace CloudAE.Core.Geometry
{
	public class UQuantization3D : Quantization3D
	{
		protected override Type SupportedPointType
		{
			get { return typeof(UQuantizedPoint3D); }
		}

		protected override Type SupportedExtentType
		{
			get { return typeof(UQuantizedExtent3D); }
		}

		public UQuantization3D(double sfX, double sfY, double sfZ, double oX, double oY, double oZ)
			: base(sfX, sfY, sfZ, oX, oY, oZ)
		{
		}

		public UQuantization3D(BinaryReader reader)
			: base(reader)
		{
		}

		protected override IQuantizedPoint3D ConvertInternal(Point3D point)
		{
			return new UQuantizedPoint3D(point);
		}

		protected override IQuantizedExtent3D ConvertInternal(Extent3D extent)
		{
			return new UQuantizedExtent3D(extent);
		}
	}
}
