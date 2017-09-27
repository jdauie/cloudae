using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Jacere.Data.PointCloud.Server
{
    internal class Program
    {
        [CommandOption("collapse-ratio", "r")]
        private static double CollapseRatio { get; set; }

        [CommandOption("collapse-limit", "l")]
        private static int CollapseLimit { get; set; }

        [CommandOption("max-tree-depth", "d")]
        private static int MaxTreeDepth { get; set; }

        [CommandOption("max-tree-nodes", "n")]
        private static int MaxTreeNodes { get; set; }

        [CommandOption("draw-density-map", "q")]
        private static bool DrawPoints { get; set; }

        private static void Main()
        {
            CommandOptionThing.ProcessStuff(typeof(Program));

            //Console.WriteLine($"{nameof(CollapseRatio)} = {CollapseRatio}");
            //Console.WriteLine($"{nameof(CollapseLimit)} = {CollapseLimit}");
            //Console.WriteLine($"{nameof(MaxTreeDepth)} = {MaxTreeDepth}");
            //Console.WriteLine($"{nameof(MaxTreeNodes)} = {MaxTreeNodes}");
            //Console.WriteLine($"{nameof(DrawPoints)} = {DrawPoints}");
            Console.WriteLine("----");

            //var drawPoints = args.Contains("--draw-density-map");

            var testFiles = new []
            {
                //@"C:\tmp\data\points.xyz",
                //@"C:\tmp\data\Site_20_golden_bucket.pts",
                //@"C:\tmp\data\old\45122D5116.txt",
                //@"C:\tmp\data\old\Hfx_Drtmth1_proj.las",
                //@"C:\tmp\data\old\Hfx_Drtmth1_proj.txt",
                //@"C:\tmp\data\old\points_a1_Kabul_tile15a.las",
                //@"C:\tmp\data\old\points_a1_Kabul_tile15a.txt",
                //@"C:\tmp\data\old\TO_core_last.las",
                //@"C:\tmp\data\old\TO_core_last.txt",
                //@"C:\tmp\data\old\0207_stadium.las",
                //@"C:\tmp\data\old\0207_stadium.txt",
                //@"C:\tmp\data\old\519_223.las",
                //@"C:\tmp\data\old\519_223.txt",
                @"C:\tmp\data\old\CRB-10-Jul_937m_f0.las",
            };

            var sw = Stopwatch.StartNew();

            foreach (var file in testFiles)
            {
                //ScanFileIndexed(file);
                //ScanFile(file);
                //ScanFileIndexed(file);
                //ScanFile(file);
                IndexFile(file, DrawPoints);
            }

            Console.WriteLine();
            Console.WriteLine($"batch completed in {sw.ElapsedMilliseconds}");

            //Console.ReadKey();

            //var listener = new HttpListener();

            //var prefixes = new[] { "http://localhost:8182/" };

            //foreach (string s in prefixes)
            //{
            //    listener.Prefixes.Add(s);
            //}

            //listener.Start();
            //Console.WriteLine("Listening...");
            //HttpListenerContext context = listener.GetContext();
            //HttpListenerRequest request = context.Request;
            //HttpListenerResponse response = context.Response;
            //string responseString = "<HTML><BODY> Hello world!</BODY></HTML>";
            //byte[] buffer = System.Text.Encoding.UTF8.GetBytes(responseString);
            //response.ContentLength64 = buffer.Length;
            //System.IO.Stream output = response.OutputStream;
            //output.Close();
            //listener.Stop();
        }

        private static void ScanFile(string file)
        {
            var sw = Stopwatch.StartNew();

            using (var stream = new FileStreamUnbufferedSequentialRead(file))
            {
                var source = Path.GetExtension(file) == ".las"
                    ? (IPointSource)new LasFile(stream)
                    : new XyzFile(stream);

                var max = 0.0;
                var count = 0L;
                foreach (var point in source.Points())
                {
                    ++count;
                    max = Math.Max(point.Z, max);
                }

                Console.WriteLine($"read {count} in {sw.ElapsedMilliseconds}");
            }
        }

        private static void ScanFileIndexed(string file)
        {
            var sw = Stopwatch.StartNew();

            using (var stream = new FileStreamUnbufferedSequentialRead(file))
            {
                var source = Path.GetExtension(file) == ".las"
                    ? (IPointSource)new LasFile(stream)
                    : new XyzFile(stream);

                var max = 0.0;
                var count = 0L;
                foreach (var point in source.Points())
                {
                    ++count;
                    max = Math.Max(point.Z, max);
                }

                Console.WriteLine($"read {count} indexed in {sw.ElapsedMilliseconds}");
            }
        }

        private static void IndexFile(string file, bool drawPoints)
        {
            var sw = Stopwatch.StartNew();

            var tree = new QuadTree();
            
            using (var stream = new FileStreamUnbufferedSequentialRead(file))
            {
                var source = Path.GetExtension(file) == ".las"
                    ? (IPointSource) new LasFile(stream)
                    : new XyzFile(stream);

                foreach (var point in source.IndexedPoints())
                {
                    tree.Add(point);
                }

                tree.CollapseSmallNodes();
                
                Console.WriteLine($"{tree._root.Count} in {sw.ElapsedMilliseconds}");

                var leafNodes = tree.GetLeaves().ToList();
                var leafDensities = leafNodes.Select(x => x.Count / Math.Pow(x.Dimension, 2)).OrderBy(x => x).ToList();

                Console.WriteLine($"{leafNodes.Count} leaf nodes");
                Console.WriteLine($"{tree.GetDepth()} depth");

                Console.WriteLine("density (points per unit area)");
                Console.WriteLine($"  naive  : {tree._root.Count / ((tree._maxX - tree._minX) * (tree._maxY - tree._minY)):N6}");
                Console.WriteLine($"  median : {leafDensities[leafDensities.Count / 2]:N6}");
                Console.WriteLine($"  mean   : {leafDensities.Average():N6}");
                Console.WriteLine($"  min    : {leafDensities.First():N6}");
                Console.WriteLine($"  max    : {leafDensities.Last():N6}");

                if (drawPoints)
                {
                    DrawDensityMap(tree, source);
                    //DrawTreeDepthMap(tree);
                    //DrawTreeIndexChunkDensityMap(tree);

                    Console.WriteLine($"{tree.GetLeaves().Sum(x => x.Chunks.Count)}");

                    var hashset = new HashSet<long>();
                    foreach (var node in tree.GetLeaves())
                    {
                        hashset.UnionWith(node.Chunks);
                    }

                    Console.WriteLine($"chunks: {hashset.Count}");
                    //foreach (var chunk in hashset.OrderBy(x => x))
                    //{
                    //    Console.WriteLine(chunk);
                    //}
                }
            }
        }

        private static void DrawDensityMap(QuadTree tree, IPointSource source)
        {
            var extentRatio = (tree._maxX - tree._minX) / (tree._maxY - tree._minY);

            var maxDisplayDimension = 64;

            var width = maxDisplayDimension;
            var height = maxDisplayDimension;

            if (extentRatio > 1)
            {
                height = (int)(maxDisplayDimension / extentRatio);
            }
            else if (extentRatio < 1)
            {
                width = (int)(maxDisplayDimension * extentRatio);
            }

            var cells = new int[height, width];
            var maxCellCount = 0;

            foreach (var point in source.Points())
            {
                var x = (int)Math.Min((point.X - tree._minX) / (tree._maxX - tree._minX) * width, width - 1);
                var y = (int)Math.Min((point.Y - tree._minY) / (tree._maxY - tree._minY) * height, height - 1);

                cells[y, x]++;

                maxCellCount = Math.Max(maxCellCount, cells[y, x]);
            }

            var chars = new[] { '█', '▓', '▒', '░' };

            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    var value = cells[y, x] == 0 ? " " : chars[Math.Min(chars.Length * cells[y, x] / maxCellCount, chars.Length - 1)].ToString();
                    Console.Write($"{value}{value}");
                }
                Console.WriteLine();
            }
        }

        private static void DrawTreeDepthMap(QuadTree tree)
        {
            var extentRatio = (tree._maxX - tree._minX) / (tree._maxY - tree._minY);

            var maxDisplayDimension = 64;

            var width = maxDisplayDimension;
            var height = maxDisplayDimension;

            if (extentRatio > 1)
            {
                height = (int)(maxDisplayDimension / extentRatio);
            }
            else if (extentRatio < 1)
            {
                width = (int)(maxDisplayDimension * extentRatio);
            }

            var cells = new int[height, width];
            var maxDepth = 0;

            foreach (var node in tree.GetLeavesWithDepth())
            {
                var xStart = (int)Math.Min((node.Item1.X - tree._root.X) / (tree._root.Dimension) * width, width - 1);
                var yStart = (int)Math.Min((node.Item1.Y - tree._root.Y) / (tree._root.Dimension) * height, height - 1);

                var xEnd = (int)Math.Min((node.Item1.X + node.Item1.Dimension - tree._root.X) / (tree._root.Dimension) * width, width - 1);
                var yEnd = (int)Math.Min((node.Item1.Y + node.Item1.Dimension - tree._root.Y) / (tree._root.Dimension) * height, height - 1);

                for (var y = yStart; y < yEnd; y++)
                {
                    for (var x = xStart; x < xEnd; x++)
                    {
                        cells[y, x] = Math.Max(cells[y, x], node.Item2);
                    }
                }

                maxDepth = Math.Max(maxDepth, node.Item2);
            }

            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    var value = cells[y, x] == 0 ? " " : (maxDepth * cells[y, x] / maxDepth).ToString();
                    Console.Write($"{value}{value}");
                }
                Console.WriteLine();
            }
        }

        private static void DrawTreeIndexChunkDensityMap(QuadTree tree)
        {
            var extentRatio = (tree._maxX - tree._minX) / (tree._maxY - tree._minY);

            var maxDisplayDimension = 64;

            var width = maxDisplayDimension;
            var height = maxDisplayDimension;

            if (extentRatio > 1)
            {
                height = (int)(maxDisplayDimension / extentRatio);
            }
            else if (extentRatio < 1)
            {
                width = (int)(maxDisplayDimension * extentRatio);
            }

            var cells = new int[height, width];
            var maxChunks = 0;

            foreach (var node in tree.GetLeaves())
            {
                var xStart = (int)Math.Min((node.X - tree._root.X) / (tree._root.Dimension) * width, width - 1);
                var yStart = (int)Math.Min((node.Y - tree._root.Y) / (tree._root.Dimension) * height, height - 1);

                var xEnd = (int)Math.Min((node.X + node.Dimension - tree._root.X) / (tree._root.Dimension) * width, width - 1);
                var yEnd = (int)Math.Min((node.Y + node.Dimension - tree._root.Y) / (tree._root.Dimension) * height, height - 1);

                for (var y = yStart; y < yEnd; y++)
                {
                    for (var x = xStart; x < xEnd; x++)
                    {
                        cells[y, x] = Math.Max(cells[y, x], node.Chunks.Count);
                    }
                }

                maxChunks = Math.Max(maxChunks, node.Chunks.Count);
            }

            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    var value = cells[y, x] == 0 ? " " : (9 * cells[y, x] / maxChunks).ToString();
                    Console.Write($"{value}{value}");
                }
                Console.WriteLine();
            }
        }
    }

    class ProgramArgs
    {
        public float CollapseRatio { get; set; }
        public int CollapseLimit { get; set; }
        public int MaxTreeDepth { get; set; }
        public int MaxTreeNodes { get; set; }
        public bool DrawPoints { get; set; }
    }
}
