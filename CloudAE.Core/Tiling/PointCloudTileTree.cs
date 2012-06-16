using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CloudAE.Core.Tiling
{
	// This will be the combined tree and grid.
	// I'm not yet sure which way to serialize.
	//   probably serialize as ID, Count => where ID maps to either a grid cell or a tree node
	// IPROPERTYCONTAINER is only for testing!!!!
	class PointCloudTileTree : IEnumerable<PointCloudTile2>, IPropertyContainer
	{
		private PointCloudTileTreeNode m_rootNode;

		static PointCloudTileTree()
		{
			// test
			var tree = new PointCloudTileTree();


		}

		public PointCloudTileTree()
		{
			ushort gridX = 8;
			ushort gridY = 10;
			
			var grid = new PointCloudTile2[gridX, gridY];

			for (ushort x = 0; x < gridX; x++)
				for (ushort y = 0; y < gridY; y++)
					grid[x, y] = new PointCloudTile2(x, y);


			ushort gridTreeBasePow = (ushort)Math.Ceiling(Math.Log(Math.Max(gridX, gridY), 2));
			ushort gridTreeBaseSize = (ushort)Math.Pow(2, gridTreeBasePow);

			PointCloudTileTreeNode[][,] gridTreeLevels = new PointCloudTileTreeNode[gridTreeBasePow + 1][,];

			for (int gridTreeLevel = 0; gridTreeLevel <= gridTreeBasePow; gridTreeLevel++)
			{
				ushort gridTreeLevelSize = (ushort)(gridTreeBaseSize / Math.Pow(2, gridTreeLevel));
				var levelNodes = new PointCloudTileTreeNode[gridTreeLevelSize, gridTreeLevelSize];
				gridTreeLevels[gridTreeLevel] = levelNodes;

				for (ushort x = 0; x < gridTreeLevelSize; x++)
				{
					for (ushort y = 0; y < gridTreeLevelSize; y++)
					{
						if (gridTreeLevel == 0)
						{
							if (x < gridX && y < gridY)
								levelNodes[x, y] = new PointCloudTileTreeNode(grid[x, y]);
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

			m_rootNode = gridTreeLevels[gridTreeBasePow][0, 0];

			// this is the new tile order
			//foreach (var tile in this)
			//{
			//    Console.WriteLine("{0}", tile);
			//}
		}

		#region IEnumerable Members

		public IEnumerator<PointCloudTile2> GetEnumerator()
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

	public class PointCloudTile2
	{
		public readonly ushort X;
		public readonly ushort Y;

		public PointCloudTile2(ushort x, ushort y)
		{
			X = x;
			Y = y;
		}

		public override string ToString()
		{
			return string.Format("({0}, {1})", X, Y);
		}
	}

	class PointCloudTileTreeNode
	{
		/// <summary>
		/// | 1 | 2 |
		/// | 3 | 4 |
		/// </summary>
		public readonly PointCloudTileTreeNode[] Nodes;
		public readonly bool HasChildNodes;

		public readonly PointCloudTile2 Tile;

		public PointCloudTileTreeNode(PointCloudTile2 tile)
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
