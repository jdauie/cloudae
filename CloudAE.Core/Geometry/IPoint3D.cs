using System;

namespace CloudAE.Core.Geometry
{
	public interface IPoint3D : IPoint2D
	{
		double Z { get; }
	}

	public interface IPoint2D
	{
		double X { get; }
		double Y { get; }
	}
}
