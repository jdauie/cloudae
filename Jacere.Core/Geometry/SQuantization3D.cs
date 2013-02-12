using System;
using System.Linq;
using System.IO;

namespace Jacere.Core.Geometry
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

        public SQuantizedPoint3D Convert(Point3D point)
        {
            return new SQuantizedPoint3D((point - Offset) / ScaleFactor);
        }

        public SQuantizedExtent3D Convert(Extent3D extent)
        {
            var e = new Extent3D(
                (extent.GetMinPoint3D() - Offset) / ScaleFactor,
                (extent.GetMaxPoint3D() - Offset) / ScaleFactor
            );
            return new SQuantizedExtent3D(e);
        }
	}
}
