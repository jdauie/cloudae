using System;
using System.Collections.Generic;
using System.Linq;

namespace Jacere.Data.PointCloud.Server
{
    class QuadTree
    {
        private const int MaxNodes = 1365;//1000000; // this just can't be greater than MaxNodesBasedOnTreeDepth (because that would be nonsense)
        private const int ReduceBy = (int)(0.20 * MaxNodes);
        private const int MergeNodesUnderThisSizeAtTheEnd = 50000;
        private const int MaxTreeDepth = 10;

        private int MaxNodesBasedOnTreeDepth = (int)(Math.Pow(4, MaxTreeDepth + 1) - 1) / (4 - 1);

        public QuadTreeNode _root;
        
        public double _minX;
        public double _minY;
        public double _maxX;
        public double _maxY;

        private readonly List<IndexedPoint3D> _initialPoints = new List<IndexedPoint3D>();
        private int _nodeCount;

        public void CollapseSmallNodes()
        {
            var stack = new Stack<QuadTreeNode>();
            stack.Push(_root);

            while (stack.Count > 0)
            {
                var parent = stack.Pop();
                foreach (var node in parent.GetNodes())
                {
                    if (node.Count < MergeNodesUnderThisSizeAtTheEnd)
                    {
                        _nodeCount -= node.GetNodes().Count();
                        node.Collapse();
                    }
                    else
                    {
                        stack.Push(node);
                    }
                }
            }
        }

        public int GetDepth()
        {
            var stack = new Stack<Tuple<QuadTreeNode, int>>();
            stack.Push(new Tuple<QuadTreeNode, int>(_root, 0));

            var maxDepth = 0;

            while (stack.Count > 0)
            {
                var parent = stack.Pop();
                foreach (var node in parent.Item1.GetNodes())
                {
                    if (node.IsLeaf())
                    {
                        maxDepth = Math.Max(maxDepth, parent.Item2 + 1);
                    }
                    else
                    {
                        stack.Push(new Tuple<QuadTreeNode, int>(node, parent.Item2 + 1));
                    }
                }
            }

            return maxDepth;
        }

        public IEnumerable<QuadTreeNode> GetLeaves()
        {
            return GetLeaves(_root);
        }

        public static IEnumerable<QuadTreeNode> GetLeaves(QuadTreeNode root)
        {
            var stack = new Stack<QuadTreeNode>();
            stack.Push(root);

            while (stack.Count > 0)
            {
                var parent = stack.Pop();
                foreach (var node in parent.GetNodes())
                {
                    if (node.IsLeaf())
                    {
                        yield return node;
                    }
                    else
                    {
                        stack.Push(node);
                    }
                }
            }
        }

        public IEnumerable<Tuple<QuadTreeNode, int>> GetLeavesWithDepth()
        {
            var stack = new Stack<Tuple<QuadTreeNode, int>>();
            stack.Push(new Tuple<QuadTreeNode,int>(_root, 0));

            while (stack.Count > 0)
            {
                var parent = stack.Pop();
                foreach (var node in parent.Item1.GetNodes())
                {
                    if (node.IsLeaf())
                    {
                        yield return new Tuple<QuadTreeNode, int>(node, parent.Item2 + 1);
                    }
                    else
                    {
                        stack.Push(new Tuple<QuadTreeNode, int>(node, parent.Item2 + 1));
                    }
                }
            }
        }

        public void Add(IndexedPoint3D point)
        {
            if (_root == null && !_initialPoints.Any())
            {
                _minX = _maxX = point.X;
                _minY = _maxY = point.Y;
            }
            else
            {
                _minX = Math.Min(_minX, point.X);
                _minY = Math.Min(_minY, point.Y);

                _maxX = Math.Max(_maxX, point.X);
                _maxY = Math.Max(_maxY, point.Y);
            }

            if (_root == null)
            {
                _initialPoints.Add(point);

                // todo: min number of points or epsilon?

                if (_minX != _maxX || _minY != _maxY)
                {
                    _root = new QuadTreeNode(_minX, _minY, Math.Max(_maxX - _minX, _maxY - _minY));
                    //_root.Count = _count + 1;
                    foreach (var p in _initialPoints)
                    {
                        _root.Add(p);
                    }
                    ++_nodeCount;
                }
            }
            else
            {
                // todo: consider whether y goes up or down

                // zoom out if necessary
                while (!_root.Contains(point))
                {
                    var newX = _root.X;
                    var newY = _root.Y;

                    if (point.X < newX)
                    {
                        newX -= _root.Dimension;
                    }

                    if (point.Y < newY)
                    {
                        newY -= _root.Dimension;
                    }

                    var newRoot = new QuadTreeNode(newX, newY, _root.Dimension * 2);
                    newRoot.Count = _root.Count;
                    ++_nodeCount;

                    if (_root.X == newRoot.X && _root.Y == newRoot.Y)
                    {
                        newRoot.NW = _root;
                    }
                    else if (_root.X == newRoot.X)
                    {
                        newRoot.SW = _root;
                    }
                    else if (_root.Y == newRoot.Y)
                    {
                        newRoot.NE = _root;
                    }
                    else
                    {
                        newRoot.SE = _root;
                    }

                    _root = newRoot;
                }

                // now zoom in (until when? initial starting depth?)
                var node = _root;
                var depthRemaining = MaxTreeDepth;
                while (true)
                {
                    ++node.Count;

                    if (--depthRemaining == 0 || node.IsFinal)
                    {
                        break;
                    }

                    var halfDimension = node.Dimension / 2;
                    var nodeCenterX = node.X + halfDimension;
                    var nodeCenterY = node.Y + halfDimension;

                    if (point.X < nodeCenterX && point.Y < nodeCenterY)
                    {
                        if (node.NW == null)
                        {
                            node.NW = new QuadTreeNode(node.X, node.Y, halfDimension);
                            ++_nodeCount;
                        }
                        node = node.NW;
                    }
                    else if (point.X < nodeCenterX)
                    {
                        if (node.SW == null)
                        {
                            node.SW = new QuadTreeNode(node.X, node.Y + halfDimension, halfDimension);
                            ++_nodeCount;
                        }
                        node = node.SW;
                    }
                    else if (point.Y < nodeCenterY)
                    {
                        if (node.NE == null)
                        {
                            node.NE = new QuadTreeNode(node.X + halfDimension, node.Y, halfDimension);
                            ++_nodeCount;
                        }
                        node = node.NE;
                    }
                    else
                    {
                        if (node.SE == null)
                        {
                            node.SE = new QuadTreeNode(node.X + halfDimension, node.Y + halfDimension, halfDimension);
                            ++_nodeCount;
                        }
                        node = node.SE;
                    }
                }

                --node.Count;
                node.Add(point);
            }

            if (_nodeCount > MaxNodes)
            {
                // sort non-leaf nodes by depth and count, and find the lowest/smallest to merge

                //Console.WriteLine($"Reducing nodes at point count {_count}");

                var depth = 1;
                var nodesByDepth = new Dictionary<int, List<QuadTreeNode>>();
                nodesByDepth.Add(depth, new List<QuadTreeNode>{_root});

                while (nodesByDepth.Count == depth)
                {
                    var nodes = nodesByDepth[depth];
                    ++depth;
                    foreach (var node in nodes)
                    {
                        foreach (var child in node.GetNodes())
                        {
                            if (!child.IsLeaf())
                            {
                                if (!nodesByDepth.ContainsKey(depth))
                                {
                                    nodesByDepth.Add(depth, new List<QuadTreeNode>());
                                }
                                nodesByDepth[depth].Add(child);
                            }
                        }
                    }
                }

                var targetNodeCount = MaxNodes - ReduceBy;

                while (--depth > 0 && _nodeCount > targetNodeCount)
                {
                    var nodes = nodesByDepth[depth].OrderBy(x => x.Count);

                    foreach (var node in nodes)
                    {
                        var leafCount = node.GetNodes().Count();
                        _nodeCount -= leafCount;

                        // todo: merge indexing info

                        node.Collapse();
                    }

                    if (_nodeCount <= targetNodeCount)
                    {
                        break;
                    }
                }
            }
        }
    }
}
