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

namespace CloudAE.App
{
	/// <summary>
	/// Interaction logic for Preview3D.xaml
	/// </summary>
	public partial class Preview3D : UserControl
	{
		private const int MAX_BUFFER_SIZE_BYTES = 1 << 26; // 26->64MB
		private const int VERTEX_COUNT_FAST = 400 * 400;
		private const int VERTEX_COUNT_LARGE = 1000 * 1000;
		
		private const bool USE_HIGH_RES_TEXTURE = true;
		private const bool USE_LOW_RES_TEXTURE = true;
		
		private ProgressManager m_progressManager;
		private BackgroundWorker m_backgroundWorker;

		private PointCloudTileSource m_currentTileSource;

		private Dictionary<PointCloudTile, GeometryModel3D> m_loadedTiles;
		private Dictionary<PointCloudTile, byte[]> m_loadedTileBuffers;

		private Dictionary<PointCloudTile, GeometryModel3D> m_lowResMap;
		private Dictionary<GeometryModel3D, PointCloudTile> m_meshTileMap;

		private byte[] m_buffer;
		private Grid<uint> m_quantizedGridLowRes;
		private Grid<float> m_gridLowRes;
		private Grid<uint> m_quantizedGridHighRes;
		private Grid<float> m_gridHighRes;

		private ushort m_gridDimensionLowRes;
		private ushort m_gridDimensionHighRes;

		private PointCollection m_gridTextureCoordsLowRes;
		private PointCollection m_gridTextureCoordsHighRes;

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
					// this is cancelling fairly quickly
					// so I will leave it this way for now
					m_backgroundWorker.CancelAsync();
				}

				m_lowResMap.Clear();
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

			m_lowResMap = new Dictionary<PointCloudTile, GeometryModel3D>();
			m_meshTileMap = new Dictionary<GeometryModel3D, PointCloudTile>();
			m_loadedTiles = new Dictionary<PointCloudTile, GeometryModel3D>();
			m_loadedTileBuffers = new Dictionary<PointCloudTile, byte[]>();

			m_backgroundWorker = new BackgroundWorker();
			m_backgroundWorker.WorkerReportsProgress = true;
			m_backgroundWorker.WorkerSupportsCancellation = true;
			m_backgroundWorker.DoWork += OnBackgroundDoWork;
			m_backgroundWorker.ProgressChanged += OnBackgroundProgressChanged;
			m_backgroundWorker.RunWorkerCompleted += OnBackgroundRunWorkerCompleted;
		}

		private void OnBackgroundDoWork(object sender, DoWorkEventArgs e)
		{
			PointCloudTileSource tileSource = e.Argument as PointCloudTileSource;

			if (tileSource != null)
			{
				previewImageGrid.MouseMove -= OnViewportGridMouseMove;

				Action<string> logAction = new Action<string>(delegate(string value) { Console.WriteLine(value); });
				m_progressManager = new ProgressManager(m_backgroundWorker, e, logAction);

				//m_gridDimensionLowRes = (ushort)Math.Sqrt(VERTEX_COUNT_FAST / tileSource.TileSet.ValidTileCount);
				//m_gridDimensionHighRes = (ushort)Math.Sqrt(VERTEX_COUNT_LARGE / tileSource.TileSet.ValidTileCount);
				m_gridDimensionLowRes = (ushort)20;
				m_gridDimensionHighRes = (ushort)40;

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
						BitmapSource meshTexture = tileSource.GeneratePreviewImage(m_gridLowRes);
						ImageBrush imageBrush = new ImageBrush(meshTexture);
						imageBrush.Freeze();
						material.Brush = imageBrush;
						
						if (m_gridTextureCoordsLowRes == null)
							m_gridTextureCoordsLowRes = MeshUtils.GeneratePlanarTextureCoordinates(mesh, MathUtils.ZAxis);
						mesh.TextureCoordinates = m_gridTextureCoordsLowRes;
					}
					else
					{
						SolidColorBrush solidBrush = new SolidColorBrush(Colors.DarkKhaki);
						solidBrush.Freeze();
						material.Brush = solidBrush;
					}

					material.Freeze();
					GeometryModel3D geometryModel = new GeometryModel3D(mesh, material);
					geometryModel.Freeze();

					// add mappings
					m_meshTileMap.Add(geometryModel, tile);
					m_lowResMap.Add(tile, geometryModel);

					if (!m_progressManager.Update((float)validTileIndex / tileSource.TileSet.ValidTileCount, geometryModel))
					    break;

					++validTileIndex;
				}
			}
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

				previewImageGrid.MouseMove += OnViewportGridMouseMove;
			}
		}

		private void OnBackgroundProgressChanged(object sender, ProgressChangedEventArgs e)
		{
			GeometryModel3D model3D = e.UserState as GeometryModel3D;
			if (viewport.Children.Count > 0)
			{
				ModelVisual3D model = viewport.Children[0] as ModelVisual3D;
				if (model != null)
				{
					Model3DGroup modelGroup = model.Content as Model3DGroup;
					if (modelGroup != null)
						modelGroup.Children.Add(model3D);
				}
			}
		}

		public void LoadPreview3D()
		{
			Model3DGroup modelGroup = new Model3DGroup();

			PointCloudTileSource tileSource = CurrentTileSource;
			CloudAE.Core.Geometry.Extent3D extent = tileSource.Extent;

			DirectionalLight lightSource = new DirectionalLight(System.Windows.Media.Colors.White, new Vector3D(-1, -1, -1));
			modelGroup.Children.Add(lightSource);

			ModelVisual3D model = new ModelVisual3D();
			model.Content = modelGroup;

			Point3D lookatPoint = new Point3D(0, 0, 0);
			Point3D cameraPoint = new Point3D(0, extent.MinY - extent.MidpointY, extent.MidpointZ - extent.MinZ + extent.RangeX);
			Vector3D lookDirection = Point3D.Subtract(cameraPoint, lookatPoint);
			lookDirection.Negate();

			PerspectiveCamera camera = new PerspectiveCamera();
			camera.Position = cameraPoint;
			camera.LookDirection = lookDirection;
			camera.UpDirection = new Vector3D(0, 0, 1);
			camera.FieldOfView = 70;

			RenderOptions.SetEdgeMode(viewport, EdgeMode.Aliased);

			viewport.Camera = camera;
			viewport.Children.Add(model);

			m_backgroundWorker.RunWorkerAsync(CurrentTileSource);
		}

		private void UpdateCurrentTile(PointCloudTile tile)
		{
			if (tile.PointCount == 0)
				return;

			List<PointCloudTile> tilesToLoad = new List<PointCloudTile>();
			int pointsToLoad = 0;

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

			if (potentialTotalPoints > totalAllowedPoints)
			{
				int pointsToDrop = potentialTotalPoints - totalAllowedPoints;
				int i = 0;
				while (pointsToDrop > 0)
				{
					PointCloudTile currentTile = loadedTiles[i];
					GeometryModel3D model = m_loadedTiles[currentTile];

					m_meshTileMap.Remove(model);
					m_loadedTiles.Remove(currentTile);
					m_loadedTileBuffers.Remove(currentTile);
					((viewport.Children[0] as ModelVisual3D).Content as Model3DGroup).Children.Remove(model);

					// replace high-res tile with low-res geometry
					GeometryModel3D lowResModel = m_lowResMap[currentTile];
					((viewport.Children[0] as ModelVisual3D).Content as Model3DGroup).Children.Add(lowResModel);

					pointsToDrop -= currentTile.PointCount;
					++i;
				}
			}

			PointCloudTile[] tilesToLoadArray = tilesToLoad.ToArray();
			SortByDistanceFromTile(tilesToLoadArray, tile);
			foreach (PointCloudTile currentTile in tilesToLoadArray)
			{
				CurrentTileSource.LoadTileGrid(currentTile, m_buffer, m_gridHighRes, m_quantizedGridHighRes);
				CloudAE.Core.Geometry.Extent3D tileExtent = currentTile.Extent;
				MeshGeometry3D mesh = CurrentTileSource.GenerateMesh(m_gridHighRes, tileExtent);

				DiffuseMaterial material = new DiffuseMaterial();
				if (USE_LOW_RES_TEXTURE)
				{
					BitmapSource meshTexture = CurrentTileSource.GeneratePreviewImage(m_gridHighRes);
					ImageBrush imageBrush = new ImageBrush(meshTexture);
					imageBrush.Freeze();
					material.Brush = imageBrush;

					if(m_gridTextureCoordsHighRes == null)
						m_gridTextureCoordsHighRes = MeshUtils.GeneratePlanarTextureCoordinates(mesh, MathUtils.ZAxis);
					mesh.TextureCoordinates = m_gridTextureCoordsHighRes;
				}
				else
				{
					SolidColorBrush solidBrush = new SolidColorBrush(Colors.DarkKhaki);
					solidBrush.Freeze();
					material.Brush = solidBrush;
				}

				material.Freeze();
				GeometryModel3D geometryModel = new GeometryModel3D(mesh, material);
				geometryModel.Freeze();

				// replace low-res tile with high-res geometry
				GeometryModel3D lowResModel = m_lowResMap[currentTile];
				((viewport.Children[0] as ModelVisual3D).Content as Model3DGroup).Children.Remove(lowResModel);
				((viewport.Children[0] as ModelVisual3D).Content as Model3DGroup).Children.Add(geometryModel);

				m_meshTileMap.Add(geometryModel, currentTile);
				m_loadedTiles.Add(currentTile, geometryModel);
#warning don't bother with this for now (since I made inputBuffer into m_buffer)
				//m_loadedTileBuffers.Add(currentTile, inputBuffer);
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
