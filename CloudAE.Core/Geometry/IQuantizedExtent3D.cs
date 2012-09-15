using System;

namespace CloudAE.Core.Geometry
{
	public interface IQuantizedExtent3D
	{
		uint RangeX { get; }
		uint RangeY { get; }
		uint RangeZ { get; }

		IQuantizedPoint3D Min { get; }
		IQuantizedPoint3D Max { get; }

		Extent3D GetExtent3D();
	}
}
