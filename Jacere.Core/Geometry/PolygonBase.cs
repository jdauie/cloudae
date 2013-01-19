using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Jacere.Core.Geometry
{
	public abstract class PolygonBase<T>
	{
		protected readonly T[] m_points;

		public bool IsDegenerate
		{
			get { return m_points.Length < 3; }
		}

		protected PolygonBase(IEnumerable<T> points)
		{
			m_points = points.ToArray();
		}

		public bool Contains(T point)
		{
			// this needs to be able to handle concave shapes, possibly complex?
			return false;
		}
	}
}
