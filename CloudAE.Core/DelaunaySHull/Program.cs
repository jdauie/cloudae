using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.IO;
using System.Drawing;

/*
  copyright s-hull.org 2011
  released under the contributors beerware license

  contributors: Phil Atkin, Dr Sinclair.
*/
namespace CloudAE.Core.Delaunay
{
    class Program
    {
        static void Main(string[] args)
        {
			//Random randy = new Random(138);

			//List<Vertex> points = new List<Vertex>();

			//if (args.Length == 0)
			//{
			//    // Generate random points.
			//    SortedDictionary<int, Point> ps = new SortedDictionary<int, Point>();
			//    for (int i = 0; i < 100000; i++)
			//    {
			//        int x = randy.Next(100000);
			//        int y = randy.Next(100000);
			//        points.Add(new Vertex(x, y));
			//    }
			//}
			//else
			//{
			//    // Read a points file as used by the Delaunay Triangulation Tester program DTT
			//    // (See http://gemma.uni-mb.si/dtt/)
			//    using (StreamReader reader = new StreamReader(args[0]))
			//    {
			//        int count = int.Parse(reader.ReadLine());
			//        for (int i = 0; i < count; i++)
			//        {
			//            string line = reader.ReadLine();
			//            string[] split = line.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
			//            points.Add(new Vertex(float.Parse(split[0]), float.Parse(split[1])));
			//        }
			//    }
			//}

			//// Write the points in the format suitable for DTT
			//using (StreamWriter writer = new StreamWriter("Triangulation c#.pnt"))
			//{
			//    writer.WriteLine(points.Count);
			//    for (int i = 0; i < points.Count; i++)
			//        writer.WriteLine(String.Format("{0},{1}", points[i].x, points[i].y));
			//}

			//// Write out the data set we're actually going to triangulate
			//Triangulator angulator = new Triangulator();

			//Stopwatch watch = new Stopwatch();
			//watch.Start();
			//List<Triad> triangles = angulator.Triangulation(points, true);
			//watch.Stop();

			//Debug.WriteLine(watch.ElapsedMilliseconds + " ms");

			//// Write the triangulation results in the format suitable for DTT
			//using (StreamWriter writer = new StreamWriter("Triangulation c#.dtt"))
			//{
			//    writer.WriteLine(triangles.Count.ToString());
			//    for (int i = 0; i < triangles.Count; i++)
			//    {
			//        Triad t = triangles[i];
			//        writer.WriteLine(string.Format("{0}: {1} {2} {3} - {4} {5} {6}",
			//            i + 1,
			//            t.a, t.b, t.c,
			//            t.ab + 1, t.bc + 1, t.ac + 1));
			//    }
			//}
        }
    }
}
