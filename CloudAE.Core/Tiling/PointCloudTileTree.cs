using System;
using System.Collections.Generic;
using System.Linq;

namespace CloudAE.Core
{
	class PointCloudTileTree : IEnumerable<PointCloudTile>
	{
		private readonly PointCloudTileTreeNode m_rootNode;

		private readonly PointCloudTile[,] m_grid;

		private readonly ushort m_gridX;
		private readonly ushort m_gridY;

		private readonly ushort m_gridTreeBasePow;
		private readonly ushort m_gridTreeBaseSize;

		public PointCloudTileTree(PointCloudTile[,] grid)
		{
			m_grid = grid;

			m_gridX = (ushort)grid.GetLength(0);
			m_gridY = (ushort)grid.GetLength(1);

			m_gridTreeBasePow = (ushort)Math.Ceiling(Math.Log(Math.Max(m_gridX, m_gridY), 2));
			m_gridTreeBaseSize = (ushort)Math.Pow(2, m_gridTreeBasePow);

			var gridTreeLevels = new PointCloudTileTreeNode[m_gridTreeBasePow + 1][,];

			for (int gridTreeLevel = 0; gridTreeLevel <= m_gridTreeBasePow; gridTreeLevel++)
			{
				ushort gridTreeLevelSize = (ushort)(m_gridTreeBaseSize / Math.Pow(2, gridTreeLevel));
				var levelNodes = new PointCloudTileTreeNode[gridTreeLevelSize, gridTreeLevelSize];
				gridTreeLevels[gridTreeLevel] = levelNodes;

				for (ushort x = 0; x < gridTreeLevelSize; x++)
				{
					for (ushort y = 0; y < gridTreeLevelSize; y++)
					{
						if (gridTreeLevel == 0)
						{
							if (x < m_gridX && y < m_gridY)
								levelNodes[x, y] = new PointCloudTileTreeNode(m_grid[x, y]);
						}
						else
						{
							var previousLevelNodes = gridTreeLevels[gridTreeLevel - 1];
							int previousLevelX = x * 2;
							int previousLevelY = y * 2;

							var newNode = new PointCloudTileTreeNode(
								previousLevelNodes[previousLevelX    , previousLevelY],
								previousLevelNodes[previousLevelX + 1, previousLevelY],
								previousLevelNodes[previousLevelX    , previousLevelY + 1],
								previousLevelNodes[previousLevelX + 1, previousLevelY + 1]
							);

							if(newNode.HasChildNodes)
								levelNodes[x, y] = newNode;
						}
					}
				}
			}

			m_rootNode = gridTreeLevels[m_gridTreeBasePow][0, 0];
		}

		#region IEnumerable Members

		public IEnumerator<PointCloudTile> GetEnumerator()
		{
			var stack = new Stack<PointCloudTileTreeNode>();
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

	class PointCloudTileTreeNode
	{
		/// <summary>
		/// | 1 | 2 |
		/// | 3 | 4 |
		/// </summary>
		public readonly PointCloudTileTreeNode[] Nodes;
		public readonly bool HasChildNodes;

		public readonly PointCloudTile Tile;

		public PointCloudTileTreeNode(PointCloudTile tile)
		{
			Nodes = null;
			HasChildNodes = false;
			Tile = tile;
		}

		public PointCloudTileTreeNode(PointCloudTileTreeNode node1, PointCloudTileTreeNode node2, PointCloudTileTreeNode node3, PointCloudTileTreeNode node4)
		{
			Nodes = new PointCloudTileTreeNode[4];
			Nodes[0] = node1;
			Nodes[1] = node2;
			Nodes[2] = node3;
			Nodes[3] = node4;

			HasChildNodes = Nodes.Any(n => n != null);
		}
	}
}
