using System;
using System.Collections.Generic;
using System.Text;

/*
  copyright s-hull.org 2011
  released under the contributors beerware license

  contributors: Phil Atkin, Dr Sinclair.
*/
namespace CloudAE.Core.Delaunay
{
    public class Vertex
    {
		private const float m_nearValue = 0.001f;

		public float x, y, z;

        protected Vertex() { }

        public Vertex(float x, float y, float z) 
        {
			this.x = x;
			this.y = y;
			this.z = z;
        }

        public float distance2To(Vertex other)
        {
            float dx = x - other.x;
            float dy = y - other.y;
            return dx * dx + dy * dy;
        }

        public float distanceTo(Vertex other)
        {
            return (float)Math.Sqrt(distance2To(other));
        }

		public bool NearEquals(Vertex v)
		{
			return ((Math.Abs(v.x - x) < m_nearValue) && (Math.Abs(v.y - y) < m_nearValue));
		}

		public override bool Equals(object obj)
		{
			if (!(obj is Vertex))
				return false;

			Vertex v = (Vertex)obj;
			return (v.x == x && v.y == y);
		}

        public override string ToString()
        {
            return string.Format("({0},{1})", x, y);
        }
    }

}
