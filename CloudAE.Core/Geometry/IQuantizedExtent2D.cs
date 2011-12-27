using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CloudAE.Core.Geometry
{
	public interface IQuantizedExtent2D
	{
		uint RangeX { get; }
		uint RangeY { get; }
	}
}
