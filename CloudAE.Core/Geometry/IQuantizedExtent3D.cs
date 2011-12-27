using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CloudAE.Core.Geometry
{
	public interface IQuantizedExtent3D : IQuantizedExtent2D
	{
		uint RangeZ { get; }
	}
}
