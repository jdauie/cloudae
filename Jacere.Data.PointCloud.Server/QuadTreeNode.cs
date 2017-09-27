using System.Collections.Generic;

namespace Jacere.Data.PointCloud.Server
{
    class QuadTreeNode
    {
        private const int IndexChunkSize = (int)ByteSizesSmall.MB_1;

        public QuadTreeNode NW;
        public QuadTreeNode NE;
        public QuadTreeNode SE;
        public QuadTreeNode SW;

        public double X;
        public double Y;
        public double Dimension;

        public bool IsFinal;
        
        public long Count;

        public HashSet<long> Chunks = new HashSet<long>();

        public QuadTreeNode(double minX, double minY, double dimension)
        {
            X = minX;
            Y = minY;
            Dimension = dimension;
        }

        public void Add(IndexedPoint3D point)
        {
            ++Count;
            Chunks.Add(point.SourceOffset / IndexChunkSize);
            Chunks.Add((point.SourceOffset + point.SourceLength - 1) / IndexChunkSize);
        }

        public bool Contains(Point3D point)
        {
            return
                (point.X >= X) &&
                (point.Y >= Y) &&
                (point.X < X + Dimension) &&
                (point.Y < Y + Dimension);
        }

        public bool IsLeaf()
        {
            return NW == null && NE == null && SE == null && SW == null;
        }
        
        public IEnumerable<QuadTreeNode> GetNodes()
        {
            if (NW != null) yield return NW;
            if (NE != null) yield return NE;
            if (SE != null) yield return SE;
            if (SW != null) yield return SW;
        }

        public void Collapse()
        {
            if (IsLeaf())
            {
                return;
            }

            foreach (var node in QuadTree.GetLeaves(this))
            {
                Chunks.UnionWith(node.Chunks);
            }

            NW = null;
            NE = null;
            SE = null;
            SW = null;
            IsFinal = true;
        }

        public override string ToString()
        {
            return $"Count: {Count}";
        }
    }
}
