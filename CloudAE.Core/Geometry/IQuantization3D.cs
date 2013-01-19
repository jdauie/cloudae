using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CloudAE.Core.Geometry
{
	public interface IQuantization3D
	{
		IQuantizedPoint3D Convert(Point3D point);
		Point3D Convert(IQuantizedPoint3D point);

		IQuantizedExtent3D Convert(Extent3D extent);
		Extent3D Convert(IQuantizedExtent3D extent);
	}
}
