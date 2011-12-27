using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CloudAE.Core.Geometry
{
	public interface IPoint3D : IPoint2D
	{
		double GetZ();
	}

	public interface IPoint2D
	{
		double GetX();
		double GetY();
	}
}
