using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;

using CloudAE.Core;
using CloudAE.Core.Tools3D;
using System.Diagnostics;
using System.ComponentModel;
using System.Timers;
using System.Windows.Threading;

namespace CloudAE.App
{
	public enum TileStitchingEdge
	{
		Top = 0,
		Left,
		TopLeft
	}

	public class TileInfo3D
	{
		public readonly PointCloudTile Tile;
		public readonly int ValidTileIndex;

		public readonly GeometryModel3D LowResGeometry;
		public readonly Grid<float> LowResGrid;

		private GeometryModel3D m_currentGeometry;
		private Grid<float> m_currentGrid;

		private GeometryModel3D[] m_stitchingGeometry;
		private Grid<float>[] m_stitchingGrid;

		private bool m_stitchingHasChanged;

		public bool StitchingHasChanged
		{
			get { return m_stitchingHasChanged; }
		}

		public GeometryModel3D CurrentGeometry
		{
			get { return m_currentGeometry; }
		}

		public Grid<float> CurrentGrid
		{
			get { return m_currentGrid; }
		}

		public TileInfo3D(PointCloudTile tile, int validTileIndex, GeometryModel3D lowResGeometry, Grid<float> lowResGrid)
		{
			Tile = tile;
			ValidTileIndex = validTileIndex;

			LowResGeometry = lowResGeometry;
			LowResGrid = lowResGrid;

			m_currentGeometry = LowResGeometry;
			m_currentGrid = LowResGrid;

			m_stitchingHasChanged = false;

			m_stitchingGeometry = new GeometryModel3D[3];
			m_stitchingGrid = new Grid<float>[3];
		}

		public void UpdateGeometry(GeometryModel3D geometry, Grid<float> grid)
		{
			m_currentGeometry = geometry;
			m_currentGrid = grid;

			for (int i = 0; i < 3; i++)
			{
				m_stitchingGeometry[i] = null;
				m_stitchingGrid[i] = null;
			}

			m_stitchingHasChanged = true;
		}

		public void ClearGeometry()
		{
			UpdateGeometry(LowResGeometry, LowResGrid);
		}

		public void UpdateStitching(GeometryModel3D stitchingGeometry, Grid<float> stitchingGrid, TileStitchingEdge edge)
		{
			int edgeIndex = (int)edge;
			m_stitchingGeometry[edgeIndex] = stitchingGeometry;
			m_stitchingGrid[edgeIndex] = stitchingGrid;

			m_stitchingHasChanged = true;
		}

		public bool HasStitching(TileStitchingEdge edge)
		{
			int edgeIndex = (int)edge;
			return (m_stitchingGeometry[edgeIndex] != null);
		}

		public Model3DGroup GetNewStitching()
		{
			//if (!m_stitchingHasChanged || !m_hasStitching)
			//    return null;

			m_stitchingHasChanged = false;

			Model3DGroup model = new Model3DGroup();

			for (int i = 0; i < 3; i++)
			{
				if (m_stitchingGeometry[i] != null)
					model.Children.Add(m_stitchingGeometry[i]);
			}
			model.Freeze();

			return model;
		}
	}

	/// <summary>
	/// Interaction logic for Preview3D.xaml
	/// </summary>
	public partial class Preview3D : UserControl, ITileSourceControl
	{
		private const int MAX_BUFFER_SIZE_BYTES = (int)ByteSizesSmall.MB_128;
		private const int VERTEX_COUNT_FAST = 400 * 400;
		private const int VERTEX_COUNT_LARGE = 1000 * 1000;
		
		private const bool USE_HIGH_RES_TEXTURE = true;
		private const bool USE_LOW_RES_TEXTURE = true;

		private const bool START_ORBIT = false;
		
		private ProgressManager m_progressManager;
		private BackgroundWorker m_backgroundWorker;

		private PointCloudTileSource m_currentTileSource;

		private Dictionary<PointCloudTile, TileInfo3D> m_tileInfo;
		private Dictionary<PointCloudTile, GeometryModel3D> m_loadedTiles;
		private Dictionary<PointCloudTile, byte[]> m_loadedTileBuffers;

		//private Dictionary<PointCloudTile, Model3D> m_lowResMap;
		private Dictionary<GeometryModel3D, PointCloudTile> m_meshTileMap;

		private Model3DCollection m_tileModelCollection;
		private Model3DCollection m_stitchingModelCollection;

		private byte[] m_buffer;
		private Grid<uint> m_quantizedGridLowRes;
		private Grid<float> m_gridLowRes;
		private Grid<uint> m_quantizedGridHighRes;
		private Grid<float> m_gridHighRes;

		private ushort m_gridDimensionLowRes;
		private ushort m_gridDimensionHighRes;

		private PointCollection m_gridTextureCoordsLowRes;
		private PointCollection m_gridTextureCoordsHighRes;

		private SolidColorBrush m_solidBrush;
		private ImageBrush m_overviewTextureBrush;
		private DiffuseMaterial m_overviewMaterial;
		private Rect3D m_overallCenteredExtent;

		private Timer m_timer;

		public string DisplayName
		{
			get { return "3D"; }
		}

		public PointCloudTileSource CurrentTileSource
		{
			get
			{
				return m_currentTileSource;
			}
			set
			{
				if (m_backgroundWorker.IsBusy)
				{
					m_backgroundWorker.CancelAsync();

					while (m_backgroundWorker.IsBusy)
					{
						System.Windows.Forms.Application.DoEvents();
					}
				}

				m_tileInfo.Clear();
				//m_lowResMap.Clear();
				m_meshTileMap.Clear();
				m_loadedTiles.Clear();
				m_loadedTileBuffers.Clear();

				viewport.Children.Clear();

				if (m_currentTileSource != null)
				{
					m_currentTileSource.Close();
				}

				m_currentTileSource = value;

				if (m_currentTileSource != null)
				{
					LoadPreview3D();
				}
			}
		}

		public Preview3D()
		{
			InitializeComponent();

			m_tileInfo = new Dictionary<PointCloudTile, TileInfo3D>();
			//m_lowResMap = new Dictionary<PointCloudTile, Model3D>();
			m_meshTileMap = new Dictionary<GeometryModel3D, PointCloudTile>();
			m_loadedTiles = new Dictionary<PointCloudTile, GeometryModel3D>();
			m_loadedTileBuffers = new Dictionary<PointCloudTile, byte[]>();

			m_solidBrush = new SolidColorBrush(Colors.DarkKhaki);
			m_solidBrush.Freeze();

			m_backgroundWorker = new BackgroundWorker();
			m_backgroundWorker.WorkerReportsProgress = true;
			m_backgroundWorker.WorkerSupportsCancellation = true;
			m_backgroundWorker.DoWork += OnBackgroundDoWork;
			m_backgroundWorker.ProgressChanged += OnBackgroundProgressChanged;
			m_backgroundWorker.RunWorkerCompleted += OnBackgroundRunWorkerCompleted;

			m_timer = new Timer();
			m_timer.Interval = 10;
			m_timer.Elapsed += OnTimerElapsed;
		}

		private void OnTimerElapsed(object sender, ElapsedEventArgs e)
		{
			// orbit
			Vector3D rotAxis = MathUtils.ZAxis;
			double rotAngle = 1;
			Quaternion q = new Quaternion(rotAxis, rotAngle);

			Matrix3D t = Matrix3D.Identity;
			t.Rotate(q);

			Point3D lookatPoint = new Point3D(0, 0, 0);
			
			Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate()
			{
				PerspectiveCamera camera = viewport.Camera as PerspectiveCamera;
				Point3D cameraPoint = camera.Position;

				cameraPoint = t.Transform(cameraPoint);

				Vector3D lookDirection = lookatPoint - cameraPoint;
				lookDirection.Normalize();

				camera.Position = cameraPoint;
				camera.LookDirection = lookDirection;
			}));
		}

		private void OnBackgroundDoWork(object sender, DoWorkEventArgs e)
		{
			PointCloudTileSource tileSource = e.Argument as PointCloudTileSource;
			CloudAE.Core.Geometry.Extent3D extent = tileSource.Extent;

			m_overviewTextureBrush = new ImageBrush(tileSource.Preview);
			m_overviewTextureBrush.ViewportUnits = BrushMappingMode.Absolute;
			m_overviewTextureBrush.Freeze();

			m_overviewMaterial = new DiffuseMaterial(m_overviewTextureBrush);
			m_overviewMaterial.Freeze();

			if (tileSource != null)
			{
				previewImageGrid.MouseMove -= OnViewportGridMouseMove;

				Action<string> logAction = new Action<string>(delegate(string value) { Console.WriteLine(value); });
				m_progressManager = new ProgressManager(m_backgroundWorker, e, logAction);

				m_gridDimensionLowRes = (ushort)Math.Sqrt(VERTEX_COUNT_FAST / tileSource.TileSet.ValidTileCount);
				//m_gridDimensionHighRes = (ushort)Math.Sqrt(VERTEX_COUNT_LARGE / tileSource.TileSet.ValidTileCount);

				m_gridDimensionHighRes = (ushort)(Math.Sqrt(tileSource.TileSet.Density.MedianTileCount) / 3);

				//m_gridDimensionLowRes = (ushort)20;
				//m_gridDimensionHighRes = (ushort)40;

				m_gridTextureCoordsLowRes = null;
				m_gridTextureCoordsHighRes = null;

				CloudAE.Core.Geometry.Point3D centerOfMass = tileSource.CenterOfMass;
				m_overallCenteredExtent = new Rect3D(extent.MinX - extent.MidpointX, extent.MinY - extent.MidpointY, extent.MinZ - centerOfMass.Z, extent.RangeX, extent.RangeY, extent.RangeZ);
				
				// load tiles
				m_buffer = new byte[tileSource.TileSet.Density.MaxTileCount * tileSource.PointSizeBytes];
				KeyValuePair<Grid<uint>, Grid<float>> gridsLowRes = tileSource.GenerateGrid(tileSource.First(), m_gridDimensionLowRes);
				m_gridLowRes = gridsLowRes.Value;
				m_quantizedGridLowRes = gridsLowRes.Key;

				KeyValuePair<Grid<uint>, Grid<float>> gridsHighRes = tileSource.GenerateGrid(tileSource.First(), m_gridDimensionHighRes);
				m_gridHighRes = gridsHighRes.Value;
				m_quantizedGridHighRes = gridsHighRes.Key;

				int validTileIndex = 0;
				foreach (PointCloudTile tile in tileSource)
				{
					tileSource.LoadTileGrid(tile, m_buffer, m_gridLowRes, m_quantizedGridLowRes);
					CloudAE.Core.Geometry.Extent3D tileExtent = tile.Extent;
					MeshGeometry3D mesh = tileSource.GenerateMesh(m_gridLowRes, tileExtent);

					DiffuseMaterial material = new DiffuseMaterial();
					if (USE_LOW_RES_TEXTURE)
					{
						material.Brush = m_overviewTextureBrush;
						mesh.TextureCoordinates = MeshUtils.GeneratePlanarTextureCoordinates(mesh, m_overallCenteredExtent, MathUtils.ZAxis);
					}
					else
					{
						material.Brush = m_solidBrush;
					}

					material.Freeze();
					GeometryModel3D geometryModel = new GeometryModel3D(mesh, material);
					geometryModel.Freeze();


					TileInfo3D tileInfo = new TileInfo3D(tile, validTileIndex, geometryModel, m_gridLowRes);
					m_tileInfo.Add(tile, tileInfo);

					// add mappings
					m_meshTileMap.Add(geometryModel, tile);
					//m_lowResMap.Add(tile, geometryModel);

					if (!m_progressManager.Update((float)validTileIndex / tileSource.TileSet.ValidTileCount, geometryModel))
					    break;

					++validTileIndex;
				}


				//// test
				//foreach (double level in new double[] { centerOfMass.Z })
				//{
				//    Grid<float> grid0 = new Grid<float>(20, 20, extent, false);
				//    grid0.FillVal = (float)level;
				//    grid0.Reset();
				//    grid0.FillVal = float.MinValue;
				//    MeshGeometry3D mesh0 = tileSource.GenerateMesh(grid0, extent);
				//    DiffuseMaterial material0 = new DiffuseMaterial(m_solidBrush);
				//    material0.Freeze();
				//    GeometryModel3D geometryModel0 = new GeometryModel3D(mesh0, material0);
				//    geometryModel0.Freeze();
				//    m_progressManager.Update(1.0f, geometryModel0);
				//}


				int validStitchingIndex = 0;
				foreach (PointCloudTile tile in tileSource)
				{
					TileInfo3D tileInfo = m_tileInfo[tile];
					Model3DGroup stitchingGroup = GenerateTileStitching(tileSource, tileInfo);

					if (stitchingGroup != null)
						++validStitchingIndex;

					if (!m_progressManager.Update(1.0f, stitchingGroup))
						break;
				}
			}
		}

		private Model3DGroup GenerateTileStitching(PointCloudTileSource tileSource, TileInfo3D tileInfo)
		{
			PointCloudTile tile = tileInfo.Tile;
			MeshGeometry3D mesh = GetTileMeshGeometry(tileInfo.CurrentGeometry);
			Grid<float> grid = tileInfo.CurrentGrid;

			//Model3DGroup stitchingGroup = new Model3DGroup();

			bool hasTop = false;
			bool hasLeft = false;

			Point3D topCornerPoint = default(Point3D);
			Point3D leftCornerPoint = default(Point3D);

			Vector3D topCornerNormal = default(Vector3D);
			Vector3D leftCornerNormal = default(Vector3D);

			// connect to left tile (if available)
			if (tile.Col > 0)
			{
				PointCloudTile leftTile = tileSource.TileSet.Tiles[tile.Col - 1, tile.Row];
				TileInfo3D leftTileInfo = null;
				if (m_tileInfo.TryGetValue(leftTile, out leftTileInfo) && leftTileInfo.CurrentGrid == grid)
				{
					MeshGeometry3D leftMesh = GetTileMeshGeometry(leftTileInfo.CurrentGeometry);
					int leftPositionsStart = leftMesh.Positions.Count - grid.SizeY;
					
					hasLeft = true;
					leftCornerPoint = leftMesh.Positions[leftPositionsStart];
					leftCornerNormal = leftMesh.Normals[leftPositionsStart];

					if (!tileInfo.HasStitching(TileStitchingEdge.Left))
					{
						MeshGeometry3D stitchingMesh = new MeshGeometry3D();

						int positionCount = grid.SizeY * 2;
						Point3DCollection positions = new Point3DCollection(positionCount);
						Vector3DCollection normals = new Vector3DCollection(positionCount);

						for (int edgePosition = 0; edgePosition < grid.SizeY; edgePosition++)
						{
							positions.Add(leftMesh.Positions[leftPositionsStart + edgePosition]);
							normals.Add(leftMesh.Normals[leftPositionsStart + edgePosition]);

							positions.Add(mesh.Positions[edgePosition]);
							normals.Add(mesh.Normals[edgePosition]);
						}
						stitchingMesh.Positions = positions;
						stitchingMesh.Normals = normals;

						Int32Collection indices = new Int32Collection((grid.SizeY - 1) * 6);
						for (int i = 0; i < grid.SizeY - 1; i++)
						{
							int j = 2 * i;
							indices.Add(j);
							indices.Add(j + 1);
							indices.Add(j + 2);

							indices.Add(j + 2);
							indices.Add(j + 1);
							indices.Add(j + 3);
						}
						stitchingMesh.TriangleIndices = indices;

						stitchingMesh.TextureCoordinates = MeshUtils.GeneratePlanarTextureCoordinates(stitchingMesh, m_overallCenteredExtent, MathUtils.ZAxis);

						GeometryModel3D stitchingModel = new GeometryModel3D(stitchingMesh, m_overviewMaterial);
						stitchingModel.Freeze();

						tileInfo.UpdateStitching(stitchingModel, leftTileInfo.CurrentGrid, TileStitchingEdge.Left);
						//stitchingGroup.Children.Add(stitchingModel);
					}
				}
			}

			// connect to top tile (if available)
			if (tile.Row > 0)
			{
				PointCloudTile topTile = tileSource.TileSet.Tiles[tile.Col, tile.Row - 1];
				TileInfo3D topTileInfo = null;
				if (m_tileInfo.TryGetValue(topTile, out topTileInfo) && topTileInfo.CurrentGrid == grid)
				{
					MeshGeometry3D topMesh = GetTileMeshGeometry(topTileInfo.CurrentGeometry);

					hasTop = true;
					topCornerPoint = topMesh.Positions[grid.SizeY - 1];
					topCornerNormal = topMesh.Normals[grid.SizeY - 1];

					if (!tileInfo.HasStitching(TileStitchingEdge.Top))
					{
						MeshGeometry3D stitchingMesh = new MeshGeometry3D();

						int positionCount = grid.SizeX * 2;
						Point3DCollection positions = new Point3DCollection(positionCount);
						Vector3DCollection normals = new Vector3DCollection(positionCount);

						for (int edgePosition = 0; edgePosition < mesh.Positions.Count; edgePosition += grid.SizeY)
						{
							positions.Add(topMesh.Positions[edgePosition + grid.SizeY - 1]);
							normals.Add(topMesh.Normals[edgePosition + grid.SizeY - 1]);

							positions.Add(mesh.Positions[edgePosition]);
							normals.Add(mesh.Normals[edgePosition]);
						}
						stitchingMesh.Positions = positions;
						stitchingMesh.Normals = normals;

						Int32Collection indices = new Int32Collection((grid.SizeX - 1) * 6);
						for (int i = 0; i < grid.SizeX - 1; i++)
						{
							int j = 2 * i;

							indices.Add(j);
							indices.Add(j + 2);
							indices.Add(j + 1);

							indices.Add(j + 2);
							indices.Add(j + 3);
							indices.Add(j + 1);
						}
						stitchingMesh.TriangleIndices = indices;

						stitchingMesh.TextureCoordinates = MeshUtils.GeneratePlanarTextureCoordinates(stitchingMesh, m_overallCenteredExtent, MathUtils.ZAxis);

						GeometryModel3D stitchingModel = new GeometryModel3D(stitchingMesh, m_overviewMaterial);
						stitchingModel.Freeze();

						tileInfo.UpdateStitching(stitchingModel, topTileInfo.CurrentGrid, TileStitchingEdge.Top);
						//stitchingGroup.Children.Add(stitchingModel);
					}
				}
			}

			// connect to top left tile (if available)
			if (hasTop && hasLeft && !tileInfo.HasStitching(TileStitchingEdge.TopLeft))
			{
				PointCloudTile topleftTile = tileSource.TileSet.Tiles[tile.Col - 1, tile.Row - 1];
				TileInfo3D topleftTileInfo = null;
				if (m_tileInfo.TryGetValue(topleftTile, out topleftTileInfo))
				{
					MeshGeometry3D topleftMesh = GetTileMeshGeometry(topleftTileInfo.CurrentGeometry);
					MeshGeometry3D stitchingMesh = new MeshGeometry3D();

					Point3DCollection positions = new Point3DCollection(4);
					Vector3DCollection normals = new Vector3DCollection(4);
					{
						positions.Add(topleftMesh.Positions[topleftMesh.Positions.Count - 1]);
						normals.Add(topleftMesh.Normals[topleftMesh.Positions.Count - 1]);

						positions.Add(topCornerPoint);
						normals.Add(topCornerNormal);

						positions.Add(leftCornerPoint);
						normals.Add(leftCornerNormal);

						positions.Add(mesh.Positions[0]);
						normals.Add(mesh.Normals[0]);
					}
					stitchingMesh.Positions = positions;
					stitchingMesh.Normals = normals;

					Int32Collection indices = new Int32Collection(6);
					indices.Add(0);
					indices.Add(1);
					indices.Add(2);
					indices.Add(2);
					indices.Add(1);
					indices.Add(3);
					stitchingMesh.TriangleIndices = indices;

					stitchingMesh.TextureCoordinates = MeshUtils.GeneratePlanarTextureCoordinates(stitchingMesh, m_overallCenteredExtent, MathUtils.ZAxis);

					GeometryModel3D stitchingModel = new GeometryModel3D(stitchingMesh, m_overviewMaterial);
					stitchingModel.Freeze();

					tileInfo.UpdateStitching(stitchingModel, topleftTileInfo.CurrentGrid, TileStitchingEdge.TopLeft);
					//stitchingGroup.Children.Add(stitchingModel);
				}
			}

			return tileInfo.GetNewStitching();
		}

		private MeshGeometry3D GetTileMeshGeometry(GeometryModel3D geometryModel)
		{
			return geometryModel.Geometry as MeshGeometry3D;
		}

		private void OnBackgroundRunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
		{
			if ((e.Cancelled == true))
			{
			}
			else if (!(e.Error == null))
			{
			}
			else
			{
				Trackball trackball = new Trackball();
				trackball.EventSource = previewImageGrid;

				viewport.Camera.Transform = trackball.Transform;

				//FlyMotionController flyMotionController = new FlyMotionController();
				//flyMotionController.EventSource = previewImageGrid;

				//viewport.Camera.Transform = flyMotionController.Transform;


				previewImageGrid.MouseMove += OnViewportGridMouseMove;

				if (START_ORBIT)
					m_timer.Start();
			}
		}

		private void OnBackgroundProgressChanged(object sender, ProgressChangedEventArgs e)
		{
			Model3D child = e.UserState as Model3D;
			if (child != null && viewport.Children.Count > 0)
			{
				ModelVisual3D model = viewport.Children[0] as ModelVisual3D;
				if (model != null)
				{
					Model3DGroup modelGroup = model.Content as Model3DGroup;
					if (modelGroup != null)
					{
						Model3DGroup modelSubGroup = modelGroup.Children[0] as Model3DGroup;
						if (modelSubGroup.Children.Count == CurrentTileSource.TileSet.ValidTileCount)
						{
							Model3DGroup modelStitchingGroup = modelGroup.Children[1] as Model3DGroup;
							modelStitchingGroup.Children.Add(child);
						}
						else
						{
							modelSubGroup.Children.Add(child);
						}
					}
				}
			}
		}

		public void LoadPreview3D()
		{
			PointCloudTileSource tileSource = CurrentTileSource;
			CloudAE.Core.Geometry.Extent3D extent = tileSource.Extent;

			Model3DGroup modelGroup = new Model3DGroup();

			Model3DGroup modelSubGroup = new Model3DGroup();
			modelGroup.Children.Add(modelSubGroup);

			Model3DGroup modelStitchingGroup = new Model3DGroup();
			modelGroup.Children.Add(modelStitchingGroup);

			DirectionalLight lightSource = new DirectionalLight(System.Windows.Media.Colors.White, new Vector3D(-1, -1, -1));
			modelGroup.Children.Add(lightSource);

			ModelVisual3D model = new ModelVisual3D();
			model.Content = modelGroup;

			CloudAE.Core.Geometry.Point3D centerOfMass = tileSource.CenterOfMass;
			Point3D lookatPoint = new Point3D(0, 0, 0);
			Point3D cameraPoint = new Point3D(0, extent.MinY - centerOfMass.Y, centerOfMass.Z - extent.MinZ + extent.RangeX);
			Vector3D lookDirection = lookatPoint - cameraPoint;
			lookDirection.Normalize();

			PerspectiveCamera camera = new PerspectiveCamera();
			camera.Position = cameraPoint;
			camera.LookDirection = lookDirection;
			camera.UpDirection = new Vector3D(0, 0, 1);
			camera.FieldOfView = 70;

			RenderOptions.SetEdgeMode(viewport, EdgeMode.Aliased);
			//viewport.ClipToBounds = false;
			//viewport.IsHitTestVisible = false;

			viewport.Camera = camera;
			viewport.Children.Add(model);

			m_tileModelCollection = modelSubGroup.Children;
			m_stitchingModelCollection = modelStitchingGroup.Children;

			m_backgroundWorker.RunWorkerAsync(CurrentTileSource);
		}

		private void UpdateCurrentTile(PointCloudTile tile)
		{
			if (tile.PointCount == 0)
				return;

			List<PointCloudTile> tilesToLoad = new List<PointCloudTile>();
			int pointsToLoad = 0;

			Model3DGroup emptyModelGroup = new Model3DGroup();
			emptyModelGroup.Freeze();

			bool isDirty = false;

			int radius = 2;

			int xMin = Math.Max(0, tile.Col - radius);
			int xMax = Math.Min(tile.Col + radius + 1, CurrentTileSource.TileSet.Cols);
			int yMin = Math.Max(0, tile.Row - radius);
			int yMax = Math.Min(tile.Row + radius + 1, CurrentTileSource.TileSet.Rows);
			for (int x = xMin; x < xMax; x++)
			{
				for (int y = yMin; y < yMax; y++)
				{
					PointCloudTile currentTile = CurrentTileSource.TileSet.Tiles[x, y];

					if (currentTile.PointCount > 0)
					{
						if (!m_loadedTiles.ContainsKey(currentTile))
						{
							tilesToLoad.Add(currentTile);
							pointsToLoad += currentTile.PointCount;

							isDirty = true;
						}
					}
				}
			}

			PointCloudTile[] loadedTiles = m_loadedTiles.Keys.ToArray();
			SortByDistanceFromTile(loadedTiles, tile);
			Array.Reverse(loadedTiles);

			// drop loaded tiles that are the farthest from the center
			int totalAllowedPoints = MAX_BUFFER_SIZE_BYTES / CurrentTileSource.PointSizeBytes;
			int loadedPoints = 0;
			for (int i = 0; i < loadedTiles.Length; i++)
				loadedPoints += loadedTiles[i].PointCount;

			int potentialTotalPoints = loadedPoints + pointsToLoad;

			Dictionary<PointCloudTile, TileInfo3D> alteredTiles = new Dictionary<PointCloudTile, TileInfo3D>();

			if (potentialTotalPoints > totalAllowedPoints)
			{
				int pointsToDrop = potentialTotalPoints - totalAllowedPoints;
				int i = 0;
				while (pointsToDrop > 0)
				{
					PointCloudTile currentTile = loadedTiles[i];
					TileInfo3D tileInfo = m_tileInfo[currentTile];
					GeometryModel3D model = m_loadedTiles[currentTile];

					m_meshTileMap.Remove(model);
					m_loadedTiles.Remove(currentTile);
					//m_loadedTileBuffers.Remove(currentTile);

					// replace high-res tile with low-res geometry
					int modelIndex = tileInfo.ValidTileIndex;
					m_tileModelCollection[modelIndex] = tileInfo.LowResGeometry;
					// clear stitching
					m_stitchingModelCollection[modelIndex] = emptyModelGroup;
					tileInfo.ClearGeometry();

					alteredTiles.Add(currentTile, tileInfo);

					pointsToDrop -= currentTile.PointCount;
					++i;
				}
			}

			PointCloudTile[] tilesToLoadArray = tilesToLoad.ToArray();
			SortByDistanceFromTile(tilesToLoadArray, tile);
			foreach (PointCloudTile currentTile in tilesToLoadArray)
			{
				TileInfo3D tileInfo = m_tileInfo[currentTile];
				CurrentTileSource.LoadTileGrid(currentTile, m_buffer, m_gridHighRes, m_quantizedGridHighRes);
				CloudAE.Core.Geometry.Extent3D tileExtent = currentTile.Extent;
				MeshGeometry3D mesh = CurrentTileSource.GenerateMesh(m_gridHighRes, tileExtent);

				DiffuseMaterial material = new DiffuseMaterial();
				if (USE_HIGH_RES_TEXTURE)
				{
					material.Brush = m_overviewTextureBrush;
					mesh.TextureCoordinates = MeshUtils.GeneratePlanarTextureCoordinates(mesh, m_overallCenteredExtent, MathUtils.ZAxis);
				}
				else
				{
					material.Brush = m_solidBrush;
				}

				material.Freeze();
				GeometryModel3D geometryModel = new GeometryModel3D(mesh, material);
				geometryModel.Freeze();

				// replace low-res tile with high-res geometry
				int modelIndex = tileInfo.ValidTileIndex;
				m_tileModelCollection[modelIndex] = geometryModel;
				// clear stitching
				m_stitchingModelCollection[modelIndex] = emptyModelGroup;
				tileInfo.UpdateGeometry(geometryModel, m_gridHighRes);

				alteredTiles.Add(currentTile, tileInfo);

				m_meshTileMap.Add(geometryModel, currentTile);
				m_loadedTiles.Add(currentTile, geometryModel);
				//m_loadedTileBuffers.Add(currentTile, inputBuffer);
			}

			// in the future, I could have a list of which tiles need to be checked for stitching updates


			// go through the stitching groups and replace any empty ones with the appropriate stitching
			if (isDirty)
			{
				PointCloudTile[] alteredTileArray = alteredTiles.Keys.ToArray();
				foreach (PointCloudTile currentTile in alteredTileArray)
				{
					// this amount of clearing is excessive.  I only want to clear one of the edges

					if (currentTile.Col < CurrentTileSource.TileSet.Cols - 1)
					{
						PointCloudTile adjacentTile = CurrentTileSource.TileSet.Tiles[currentTile.Col + 1, currentTile.Row];
						if (m_tileInfo.ContainsKey(adjacentTile))
						{
							TileInfo3D adjacentTileInfo = m_tileInfo[adjacentTile];
							if (!alteredTiles.ContainsKey(adjacentTile))
								alteredTiles.Add(adjacentTile, adjacentTileInfo);

							m_stitchingModelCollection[adjacentTileInfo.ValidTileIndex] = emptyModelGroup;
							adjacentTileInfo.UpdateStitching(null, null, TileStitchingEdge.Left);
						}
					}

					if (currentTile.Row < CurrentTileSource.TileSet.Rows - 1)
					{
						PointCloudTile adjacentTile = CurrentTileSource.TileSet.Tiles[currentTile.Col, currentTile.Row + 1];
						if (m_tileInfo.ContainsKey(adjacentTile))
						{
							TileInfo3D adjacentTileInfo = m_tileInfo[adjacentTile];
							if (!alteredTiles.ContainsKey(adjacentTile))
								alteredTiles.Add(adjacentTile, adjacentTileInfo);

							m_stitchingModelCollection[adjacentTileInfo.ValidTileIndex] = emptyModelGroup;
							adjacentTileInfo.UpdateStitching(null, null, TileStitchingEdge.Top);
						}
					}

					if (currentTile.Col < CurrentTileSource.TileSet.Cols - 1 && currentTile.Row < CurrentTileSource.TileSet.Rows - 1)
					{
						PointCloudTile adjacentTile = CurrentTileSource.TileSet.Tiles[currentTile.Col + 1, currentTile.Row + 1];
						if (m_tileInfo.ContainsKey(adjacentTile))
						{
							TileInfo3D adjacentTileInfo = m_tileInfo[adjacentTile];
							if (!alteredTiles.ContainsKey(adjacentTile))
								alteredTiles.Add(adjacentTile, adjacentTileInfo);

							m_stitchingModelCollection[adjacentTileInfo.ValidTileIndex] = emptyModelGroup;
							adjacentTileInfo.UpdateStitching(null, null, TileStitchingEdge.TopLeft);
						}
					}
				}

				foreach (KeyValuePair<PointCloudTile, TileInfo3D> kvp in alteredTiles)
				{
					int i = kvp.Value.ValidTileIndex;
					Model3DGroup stitchingGroup = m_stitchingModelCollection[i] as Model3DGroup;
					if (stitchingGroup.Children.Count == 0)
					{
						GeometryModel3D geometryModel = m_tileModelCollection[i] as GeometryModel3D;
						Model3DGroup newStitchingGroup = GenerateTileStitching(CurrentTileSource, kvp.Value);
						if (newStitchingGroup.Children.Count > 0)
							m_stitchingModelCollection[i] = newStitchingGroup;
					}
				}

				//for (int i = 0; i < m_stitchingModelCollection.Count; i++)
				//{
				//    Model3DGroup stitchingGroup = m_stitchingModelCollection[i] as Model3DGroup;
				//    // this is an incorrect condition, but it won't be apparent until I get multi-res stitching
				//    if (stitchingGroup.Children.Count < 3)
				//    {
				//        GeometryModel3D geometryModel = m_tileModelCollection[i] as GeometryModel3D;
				//        PointCloudTile currentTile = m_meshTileMap[geometryModel];
				//        TileInfo3D currentTileInfo = m_tileInfo[currentTile];
				//        Model3DGroup newStitchingGroup = GenerateTileStitching(CurrentTileSource, currentTileInfo);
				//        if (newStitchingGroup.Children.Count > 0)
				//            m_stitchingModelCollection[i] = newStitchingGroup;
				//    }
				//}
			}
		}

		private void SortByDistanceFromTile(PointCloudTile[] tiles, PointCloudTile centerTile)
		{
			float[] distanceToCenter2 = new float[tiles.Length];
			for (int i = 0; i < tiles.Length; i++)
			{
				distanceToCenter2[i] = (float)(Math.Pow(tiles[i].Col - centerTile.Col, 2) + Math.Pow(tiles[i].Row - centerTile.Row, 2));
			}

			Array.Sort<float, PointCloudTile>(distanceToCenter2, tiles);
		}

		private void OnViewportGridMouseMove(object sender, MouseEventArgs e)
		{
			if (Keyboard.IsKeyDown(Key.LeftCtrl))
			{
				RayMeshGeometry3DHitTestResult result = (RayMeshGeometry3DHitTestResult)VisualTreeHelper.HitTest(viewport, e.GetPosition(viewport));

				if (result != null)
				{
					GeometryModel3D model = result.ModelHit as GeometryModel3D;
					if (model != null && m_meshTileMap.ContainsKey(model))
					{
						PointCloudTile tile = m_meshTileMap[model];

						UpdateCurrentTile(tile);
					}
				}
			}
		}
	}
}
