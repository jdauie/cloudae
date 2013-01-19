using System;
using System.Linq;
using System.IO;

namespace CloudAE.Core.Geometry
{
	public class SQuantization3D : Quantization3D
	{
		protected override Type SupportedPointType
		{
			get { return typeof(SQuantizedPoint3D); }
		}

		protected override Type SupportedExtentType
		{
			get { return typeof(SQuantizedExtent3D); }
		}

		public SQuantization3D(double sfX, double sfY, double sfZ, double oX, double oY, double oZ)
			: base(sfX, sfY, sfZ, oX, oY, oZ)
		{
		}

		public SQuantization3D(BinaryReader reader)
			: base(reader)
		{
		}

		protected override IQuantizedPoint3D ConvertInternal(Point3D point)
		{
			return new SQuantizedPoint3D(point);
		}

		protected override IQuantizedExtent3D ConvertInternal(Extent3D extent)
		{
			return new SQuantizedExtent3D(extent);
		}
	}
}
