using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using Jacere.Core;

namespace CloudAE.Core
{
    [Obsolete("Not worth the trouble")]
	class PointCloudTileTree : IEnumerable<PointCloudTile>
	{
		private readonly IPointCloudTileTreeNode m_rootNode;

		private readonly ushort m_gridX;
		private readonly ushort m_gridY;

		private readonly ushort m_gridTreeBasePow;
		private readonly ushort m_gridTreeBaseSize;

		public static IEnumerable<PointCloudTileCoord> GetTileOrderEnumerator(ushort rows, ushort cols)
		{
			var stack = new Stack<LevelXY>();

			// start at the top level
			ushort basePow = (ushort)Math.Ceiling(Math.Log(Math.Max(rows, cols), 2));
			stack.Push(new LevelXY(basePow, 0, 0));

			while (stack.Count > 0)
			{
				var levelxy = stack.Pop();

				if (levelxy.Level == 1)
				{
					// return children (in order)
					for (ushort y = levelxy.Y; y < levelxy.Y + 2; y++)
						for (ushort x = levelxy.X; x < levelxy.X + 2; x++)
							if (x < cols && y < rows)
								yield return new PointCloudTileCoord(y, x);
				}
				else
				{
					ushort jump = (ushort)Math.Pow(2, levelxy.Level - 1);

					// go through the current level children (in reverse to maintain the stack)
					for (int y = 1; y >= 0; y--)
						for (int x = 1; x >= 0; x--)
							stack.Push(new LevelXY((ushort)(levelxy.Level - 1), (ushort)(levelxy.Y + jump * y), (ushort)(levelxy.X + jump * x)));
				}
			}

			//ushort basePow = (ushort)Math.Ceiling(Math.Log(Math.Max(rows, cols), 2));
			//ushort baseSize = (ushort)Math.Pow(2, basePow);
			//int totalBaseCount = baseSize * baseSize;
			//for (int i = 0; i < totalBaseCount; i++)
			//{

			//    // start with 2 ^ (basePow + 1)
				
			//}
		}

		public PointCloudTileTree(PointCloudTile[,] grid)
		{
			m_gridX = (ushort)grid.GetLength(0);
			m_gridY = (ushort)grid.GetLength(1);

			m_gridTreeBasePow = (ushort)Math.Ceiling(Math.Log(Math.Max(m_gridX, m_gridY), 2));
			m_gridTreeBaseSize = (ushort)Math.Pow(2, m_gridTreeBasePow);

			var gridTreeLevels = new IPointCloudTileTreeNode[m_gridTreeBasePow + 1][,];
			var quadTemp = new IPointCloudTileTreeNode[4];

			for (int gridTreeLevel = 0; gridTreeLevel <= m_gridTreeBasePow; gridTreeLevel++)
			{
				ushort gridTreeLevelSize = (ushort)(m_gridTreeBaseSize / Math.Pow(2, gridTreeLevel));
				var levelNodes = new IPointCloudTileTreeNode[gridTreeLevelSize, gridTreeLevelSize];
				gridTreeLevels[gridTreeLevel] = levelNodes;

				for (ushort y = 0; y < gridTreeLevelSize; y++)
				{
					for (ushort x = 0; x < gridTreeLevelSize; x++)
					{
						if (gridTreeLevel == 0)
						{
							var tile = grid[y, x];
							if (tile != null)
								levelNodes[y, x] = new PointCloudTileTreeNodeLeaf(grid[y, x]);
						}
						else
						{
							var previousLevelNodes = gridTreeLevels[gridTreeLevel - 1];
							int previousLevelX = x * 2;
							int previousLevelY = y * 2;

							quadTemp[0] = previousLevelNodes[previousLevelY    , previousLevelX    ];
							quadTemp[1] = previousLevelNodes[previousLevelY    , previousLevelX + 1];
							quadTemp[2] = previousLevelNodes[previousLevelY + 1, previousLevelX    ];
							quadTemp[3] = previousLevelNodes[previousLevelY + 1, previousLevelX + 1];

							if (quadTemp.Any(n => n != null))
								levelNodes[y, x] = new PointCloudTileTreeNode(quadTemp);
						}
					}
				}
			}

			m_rootNode = gridTreeLevels[m_gridTreeBasePow][0, 0];
		}

		public PointCloudTile GetTile(ushort row, ushort col)
		{
			//var node = m_rootNode;

			


			return null;
		}

		#region IEnumerable Members

		public IEnumerator<PointCloudTile> GetEnumerator()
		{
			var stack = new Stack<IPointCloudTileTreeNode>();
			stack.Push(m_rootNode);

			while (stack.Count > 0)
			{
				var current = stack.Pop();
				if (current.HasChildNodes)
				{
					for (int i = current.Nodes.Length - 1; i >= 0; i--)
					{
						if (current.Nodes[i] != null)
							stack.Push(current.Nodes[i]);
					}
				}
				else
				{
					yield return current.Tile;
				}
			}
		}

		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		#endregion
	}

	internal struct LevelXY
	{
		public readonly ushort Level;
		public readonly ushort X;
		public readonly ushort Y;

		public LevelXY(ushort level, ushort y, ushort x)
		{
			Level = level;
			Y = y;
			X = x;
		}
	}

	interface IPointCloudTileTreeNode
	{
		bool HasChildNodes { get; }
		IPointCloudTileTreeNode[] Nodes { get; }
		PointCloudTile Tile { get; }
	}

	class PointCloudTileTreeNode : IPointCloudTileTreeNode
	{
		/// <summary>
		/// | 1 | 2 |
		/// | 3 | 4 |
		/// </summary>
		private readonly IPointCloudTileTreeNode[] m_nodes;

		public bool HasChildNodes
		{
			get { return true; }
		}

		public IPointCloudTileTreeNode[] Nodes
		{
			get { return m_nodes; }
		}

		public PointCloudTile Tile
		{
			get { return null; }
		}

		public PointCloudTileTreeNode(IPointCloudTileTreeNode[] nodes)
		{
			m_nodes = new IPointCloudTileTreeNode[4];
			for (int i = 0; i < 4; i++)
				m_nodes[i] = nodes[i];
		}
	}

	class PointCloudTileTreeNodeLeaf : IPointCloudTileTreeNode
	{
		private readonly PointCloudTile m_tile;

		public bool HasChildNodes
		{
			get { return false; }
		}

		public IPointCloudTileTreeNode[] Nodes
		{
			get { return null; }
		}

		public PointCloudTile Tile
		{
			get { return m_tile; }
		}

		public PointCloudTileTreeNodeLeaf(PointCloudTile tile)
		{
			m_tile = tile;
		}
	}

	interface ITileCoord
	{
		ushort Row { get; }
		ushort Col { get; }
	}

	public struct PointCloudTileCoord : ISerializeBinary, ITileCoord, IEquatable<PointCloudTileCoord>
	{
		private readonly ushort m_row;
		private readonly ushort m_col;

		public static int GetIndex(ushort row, ushort col)
		{
			return ((row << 16) | col);
		}

		public static int GetIndex(int row, int col)
		{
			return ((row << 16) | col);
		}

        public static int GetIndex(IGrid grid, int incrementalIndex)
        {
            var row = incrementalIndex / grid.SizeX;
            var col = incrementalIndex % grid.SizeX;

            return ((row << 16) | col);
        }

		public static PointCloudTileCoord Empty
		{
			get { return new PointCloudTileCoord(ushort.MaxValue, ushort.MaxValue); }
		}

		public ushort Row
		{
			get { return m_row; }
		}

		public ushort Col
		{
			get { return m_col; }
		}

		public int Index
		{
			get { return ((m_row << 16) | m_col); }
		}

		public bool IsEmpty
		{
			get { return Equals(Empty); }
		}

		public PointCloudTileCoord(ushort y, ushort x)
		{
			m_row = y;
			m_col = x;
		}

		public PointCloudTileCoord(uint index)
		{
			m_row = (ushort)(index >> 16);
			m_col = (ushort)(index | ((1 << 16) - 1));
		}

		public PointCloudTileCoord(BinaryReader reader)
		{
			m_row = reader.ReadUInt16();
			m_col = reader.ReadUInt16();
		}

		public void Serialize(BinaryWriter writer)
		{
			writer.Write(m_row);
			writer.Write(m_col);
		}

		public override int GetHashCode()
		{
			return Index;
		}

		public override bool Equals(object obj)
		{
			return Equals((PointCloudTileCoord)obj);
		}

		public bool Equals(PointCloudTileCoord other)
		{
			return (other.m_col == m_col && other.m_row == m_row);
		}

		public static bool operator ==(PointCloudTileCoord c1, PointCloudTileCoord c2)
		{
			return c1.Equals(c2);
		}

		public static bool operator !=(PointCloudTileCoord c1, PointCloudTileCoord c2)
		{
			return !c1.Equals(c2);
		}

		public override string ToString()
		{
			return string.Format("({0}, {1})", m_row, m_col);
		}
	}
}
