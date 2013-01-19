using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Jacere.Core.Geometry;

namespace CloudAE.Core.DelaunayIncremental
{
	class Delaunay2DIncremental
	{
		private const int NUM_STEPS_TO_LOCATE = 3000;

		private Triangle m_currentTriangle;

		Triangle m_delaunayGraph; //holds the delaunay graph

		//to hold the vertices of the super Triangle_t, useful while printing the results
		DelaunayPoint m_superA;
		DelaunayPoint m_superB;
		DelaunayPoint m_superC;

		List<int> m_outputTriangles;

		List<Triangle> m_newTriangles;

		Extent3D m_extent;

		public void Initialize(Extent3D extent, int numPointsToProcess)
		{
			double SQRT3 = Math.Sqrt(3);

			//create one big Triangle_t aka. supertriangle
			//Make the bounding box slightly bigger, 
			//actually no need for this if coming from streaming delaunay side

			m_extent = extent;

			// compute the supertriangle vertices, clockwise order, check the math
			m_superA = new DelaunayPoint(extent.MinX - extent.RangeY * SQRT3 / 3.0f, extent.MinY, extent.MinZ, numPointsToProcess);
			m_superB = new DelaunayPoint(extent.MidpointX, extent.MaxY + extent.RangeX * SQRT3 * 0.5f, extent.MinZ, numPointsToProcess + 1);
			m_superC = new DelaunayPoint(extent.MaxX + extent.RangeY * SQRT3 / 3.0f, extent.MinY, extent.MinZ, numPointsToProcess + 2);

			//create the super Triangle_t
			m_delaunayGraph = new Triangle(m_superA, m_superB, m_superC);
			
			//keep track of the current Triangle_t
			m_currentTriangle = m_delaunayGraph;

			m_newTriangles = new List<Triangle>();
			m_outputTriangles = new List<int>();
		}

		public List<int> FlushTriangles()
		{
			//if(m_currentTriangle == null)
			//    return null;

			m_currentTriangle = null;
			foreach(Triangle triangle in m_newTriangles)
			{
				//if (m_extent.Contains(triangle.CircumCircleExtent())) // Check to see if triangle's CC extent is in cell.
				//{
				//    FreeTriangle(triangle, true);
				//}
				//else
				//{
				//    triangle.visited = true;
				//    m_currentTriangle = triangle;
				//}

				FreeTriangle(triangle, true);
			}
			m_newTriangles.Clear();

			return m_outputTriangles;
		}

		void FreeTriangle(Triangle t, bool flush)
		{
			if(t == null)
			{
				//should never come here, maybe the last ones
				return;
			}

			//make sure we arent deleting the m_currentTriangle
			if(m_currentTriangle == t)
				m_currentTriangle = null;

			// TODO: Encapsulate this later if triangles reference edges/vertices via shared_ptr.
			DelaunayPoint v1 = t.m_v[0];
			DelaunayPoint v2 = t.m_v[1];
			DelaunayPoint v3 = t.m_v[2];

			if(flush)
			{
				bool isSuperTriangleVertex = false;
				for(int i = 0; i < 3; i++)
				{
					DelaunayPoint v = t.m_v[i];
					if(v == m_superA || v == m_superB || v == m_superC)
					{
						isSuperTriangleVertex = true;
						break;
					}
				}

				if(!isSuperTriangleVertex)
				{
					m_outputTriangles.Add(v1.Index);
					m_outputTriangles.Add(v2.Index);
					m_outputTriangles.Add(v3.Index);
				}
			}

			//m_trianglesMemoryManager.dealloc(*t);
		}

		public bool UpdateTriangle(DelaunayPoint p)
		{
			if (m_currentTriangle == null)
				return false;

			if (m_currentTriangle.IsCoincidentWithVertex(p))
				return false;

			Update(p);

			return true;
		}

		void Update(DelaunayPoint p)
		{
			List<Triangle> removeList = new List<Triangle>();
			List<Triangle> visitedList = new List<Triangle>();

			// Go in dfs order and keep adding triangles to it until the vertices of Triangle dont fall in the circumcircle
			m_currentTriangle.remove = true;
			m_currentTriangle.visited = true;
			removeList.Add(m_currentTriangle);

			for (int start = 0; start != removeList.Count; ++start)
			{
				// All triangles with circumcircle containing the point will need to be removed.
				Triangle remTriangle = removeList[start];
				for(int k = 0; k < 3; k++)
				{
					Triangle adjacentTriangle = remTriangle.adjacentFace[k];
					if(adjacentTriangle != null && !adjacentTriangle.visited )
					{
						adjacentTriangle.visited = true;

						if(adjacentTriangle.CircumCircleContains(p))
						{
							adjacentTriangle.remove = true;
							removeList.Add(adjacentTriangle); //holds a list of removeable Triangle_t
						}
						else
						{
							visitedList.Add(adjacentTriangle);
						}
					}
				}
			}

			if (visitedList.Count > 0)
			{
				for(int i = 0; i < visitedList.Count; i++)
					visitedList[i].visited = false;
			}
			visitedList.Clear();

			// For every n triangles we are going to remove we should be able to add n+2 new ones
			int numNewTriangles = 0;
			for(int i = 0; i != removeList.Count; i++)
			{
				Triangle remTriangle = removeList[i];
				for(int j = 0; j != 3; j++)
				{
					// Create a Triangle for each side that isnt to be removed.
					if(remTriangle.adjacentFace[j] == null)
					{
						numNewTriangles++;
					}
					else if(!remTriangle.adjacentFace[j].remove) 
					{
						numNewTriangles++;
					}
				}
			}

			// Paranoia check.
			if(numNewTriangles - 2 != removeList.Count)
			{
				// Don't expect to hit this, but checking anyways just in case of rounding errors.
				for(int i = 0; i != removeList.Count; i++)
				{
					removeList[i].visited = false;
					removeList[i].remove = false;
				}

				return;
			}

			// Create new triangles.
			List<Triangle> newList = new List<Triangle>();
			int[] vertIndex = new int[] { 1, 2, 0 };

			for(int i = 0; i != removeList.Count; i++)
			{
				Triangle remTriangle = removeList[i];
				for(int j = 0; j < 3; j++)
				{
					//create a Triangle for each side that isnt to be removed
					Triangle adjacent = remTriangle.adjacentFace[j];
					if(adjacent == null)
					{
						Triangle t = new Triangle(remTriangle.m_v[j], remTriangle.m_v[vertIndex[j]], p);
						newList.Add(t);
					}
					else if(!adjacent.remove)
					{
						Triangle t = new Triangle(remTriangle.m_v[j], remTriangle.m_v[vertIndex[j]], p);
						
						// Stitch shared edge of new triangle into existing network.
						t.adjacentFace[0] = adjacent;
						for (int e = 0; e != 3; e++)
						{
							if (adjacent.adjacentFace[e] == remTriangle)
								adjacent.adjacentFace[e] = t;
						}

						newList.Add(t);
					}
				}
			}

			// Stitch up the new triangles with each-other.  Simplified case because vertex[2] for all shared triangles is the same, and is
			// in fact parameter point 'p'.
			m_currentTriangle = null;
			for(int i = 0; i != newList.Count; i++)
			{
				Triangle t = newList[i];
				m_currentTriangle = t;

				// Iterate over other un-stitched edges looking for a match.
				for(int j = i+1; (j != newList.Count) && ((t.adjacentFace[1] == null) || (t.adjacentFace[2] == null)); j++)
				{
					Triangle q = newList[j];
					if (t.adjacentFace[1] == null)
					{
						if(t.m_v[1] == q.m_v[0])
						{
							t.adjacentFace[1] = q;
							q.adjacentFace[2] = t;
						}
					}

					if (t.adjacentFace[2] == null)
					{
						if(t.m_v[0] == q.m_v[1])
						{
							t.adjacentFace[2] = q;
							q.adjacentFace[1] = t;
						}
					}
				}
			}

			// Free triangles that were to be removed.
			for(int i = 0; i != removeList.Count; i++)
			{
				Triangle cur = removeList[i];
				m_newTriangles.Remove(cur);
				FreeTriangle(cur, false);
			}

			// Add new triangles for later consideration & processing.
			for (int i = 0; i != newList.Count; ++i)
				m_newTriangles.Add(newList[i]);
		}

		public bool Locate(DelaunayPoint p)
		{
			// Attempt to walk to the triangle.
			if(LocateInternal(p)) 
			{
				return true;
			}
			else
			{
				//// Attempt to walk to the triangle using the cell's boundary triangles.
				//if(LocateInternalUsingTriangleList(cell->BoundaryTriangles(), p))
				//{
				//    return true;
				//}

				// Last ditch effort to find the triangle looking through memory -- O(n)
				return LocateInternalBruteForce(p);
			}
		}

		bool LocateInternal(DelaunayPoint p)
		{
			if(m_currentTriangle != null)
			{
				Triangle newTriangle = LocateInternal(m_currentTriangle, p);
				if(newTriangle != null)
					return true;
			}

			return false;
		}

		Triangle LocateInternal(Triangle startTriangle, DelaunayPoint end)
		{
			int step = 0;

			//first check if the point is in the current Triangle_t
			while((startTriangle != null) && !startTriangle.Contains(end, ref startTriangle) && (++step) <= NUM_STEPS_TO_LOCATE)
			{
			}

			if(step > NUM_STEPS_TO_LOCATE)
				startTriangle = null;

			if(startTriangle == null)
				return null;

			m_currentTriangle = startTriangle;	
			return m_currentTriangle;
		}


		bool LocateInternalUsingTriangleList(IEnumerable<Triangle> triangles, DelaunayPoint p)
		{
			foreach(Triangle triangle in triangles)
			{
				if (LocateInternal(triangle, p) != null)
					return true;
			}

			return false;
		}

		bool LocateInternalBruteForce(DelaunayPoint p)
		{
			m_currentTriangle = null;

			foreach (Triangle triangle in m_newTriangles)
			{
				if (triangle.Contains(p))
				{
					m_currentTriangle = triangle;
					return true;
				}
			}

			return false;
		}
	}
}
