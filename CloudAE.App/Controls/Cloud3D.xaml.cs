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
	/// <summary>
	/// Interaction logic.
	/// </summary>
	public partial class Cloud3D : UserControl, ITileSourceControl
	{
		private ProgressManager m_progressManager;
		private ManagedBackgroundWorker m_backgroundWorker;

		private PointCloudTileSource m_currentTileSource;

		private byte[] m_buffer;
		
		private ImageBrush m_overviewTextureBrush;
		private DiffuseMaterial m_overviewMaterial;
		private Rect3D m_overallCenteredExtent;

		public string DisplayName
		{
			get { return "Cloud"; }
		}

		public int Index
		{
			get { return 2; }
		}

		public ImageSource Icon
		{
			get { return new BitmapImage(new Uri("pack://application:,,,/CloudAE.App;component/Icons/weather_clouds.png")); }
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

				viewport.Children.Clear();

				m_currentTileSource = value;

				if (m_currentTileSource != null)
				{
					LoadPreview3D();
				}
			}
		}

		public Cloud3D()
		{
			InitializeComponent();

			this.IsEnabled = false;

			m_backgroundWorker = new ManagedBackgroundWorker();
			m_backgroundWorker.WorkerReportsProgress = true;
			m_backgroundWorker.WorkerSupportsCancellation = true;
			m_backgroundWorker.DoWork += OnBackgroundDoWork;
			m_backgroundWorker.ProgressChanged += OnBackgroundProgressChanged;
			m_backgroundWorker.RunWorkerCompleted += OnBackgroundRunWorkerCompleted;
		}

		private void OnBackgroundDoWork(object sender, DoWorkEventArgs e)
		{
			PointCloudTileSource tileSource = e.Argument as PointCloudTileSource;
			CloudAE.Core.Geometry.Extent3D extent = tileSource.Extent;

			m_overviewTextureBrush = new ImageBrush(tileSource.Preview.Image);
			m_overviewTextureBrush.ViewportUnits = BrushMappingMode.Absolute;
			m_overviewTextureBrush.Freeze();

			m_overviewMaterial = new DiffuseMaterial(m_overviewTextureBrush);
			m_overviewMaterial.Freeze();

			if (tileSource != null)
			{
				Action<string> logAction = new Action<string>(delegate(string value) { Context.WriteLine(value); });
				m_progressManager = new BackgroundWorkerProgressManager(m_backgroundWorker, e, logAction);

				CloudAE.Core.Geometry.Point3D centerOfMass = tileSource.CenterOfMass;
				m_overallCenteredExtent = new Rect3D(extent.MinX - extent.MidpointX, extent.MinY - extent.MidpointY, extent.MinZ - centerOfMass.Z, extent.RangeX, extent.RangeY, extent.RangeZ);
				
				m_buffer = new byte[tileSource.TileSet.Density.MaxTileCount * tileSource.PointSizeBytes];

				int thinFactor = (int)Math.Ceiling((double)tileSource.Count / 1000000);
				int thinnedPointCount = (int)(tileSource.Count / thinFactor);
				double areaPerPoint = tileSource.Extent.Area / thinnedPointCount;
				double sqrtAreaPerPoint = Math.Sqrt(areaPerPoint);
				
				foreach (PointCloudTile tile in tileSource.TileSet.ValidTiles)
				{
					MeshGeometry3D mesh = tileSource.LoadTilePointMesh(tile, m_buffer, sqrtAreaPerPoint, thinFactor);

					DiffuseMaterial material = new DiffuseMaterial();
					material.Brush = m_overviewTextureBrush;
					mesh.TextureCoordinates = MeshUtils.GeneratePlanarTextureCoordinates(mesh, m_overallCenteredExtent, MathUtils.ZAxis);
					material.Freeze();
					GeometryModel3D geometryModel = new GeometryModel3D(mesh, material);
					geometryModel.Freeze();

					if (!m_progressManager.Update(tile, geometryModel))
					    break;
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
						modelSubGroup.Children.Add(child);
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

			m_backgroundWorker.RunWorkerAsync(CurrentTileSource);
		}
	}
}
