using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

using CloudAE.Core;
using Jacere.Core;

namespace CloudAE.App
{
	/// <summary>
	/// Interaction logic for Preview3Db.xaml
	/// </summary>
	public sealed partial class Preview3Db : UserControl, ITileSourceControl, IFactory
	{
		private const int MAX_BUFFER_SIZE_BYTES = (int)ByteSizesSmall.MB_128;

	    private const int MAX_COLOR_QUALITY = 100;

		private static readonly ColorRamp[] c_colorRamps;

		private PointCloudTileSource m_currentTileSource;

		public string DisplayName
		{
			get { return "3Db"; }
		}

		public int Index
		{
			get { return 0; }
		}

		public ImageSource Icon
		{
			get { return new BitmapImage(new Uri("pack://application:,,,/CloudAE.App;component/Icons/map.png")); }
		}

		public PointCloudTileSource CurrentTileSource
		{
			get
			{
				return m_currentTileSource;
			}
			set
			{
				if (m_currentTileSource != null)
				{
					colorRampsCombo.SelectedItem = null;
				}

				m_currentTileSource = value;
                viewport.Children.Clear();

				if (m_currentTileSource != null)
				{
					colorRampsCombo.SelectedItem = CurrentColorRamp;
					colorUseStdDev.IsChecked = CurrentColorUseStdDev;
				}
				else
				{
					previewImage.Source = null;
				}
			}
		}

		private ColorRamp CurrentColorRamp
		{
			get
			{
				var ramp = ColorRamp.PredefinedColorRamps.Elevation1;
				if (CurrentTileSource.Preview != null)
					ramp = m_currentTileSource.Preview.ColorHandler as ColorRamp;

				return ramp;
			}
		}

		private bool CurrentColorUseStdDev
		{
			get
			{
				bool useStdDev = true;
				if (CurrentTileSource.Preview != null)
					useStdDev = CurrentTileSource.Preview.UseStdDevStretch;

				return useStdDev;
			}
		}

		static Preview3Db()
		{
			List<ColorRamp> controls = RegisterColorHandlers();
			c_colorRamps = controls.ToArray();
		}
		
		public Preview3Db()
		{
			InitializeComponent();

			foreach (ColorRamp handler in c_colorRamps)
				colorRampsCombo.Items.Add(handler);
		}

		private static List<ColorRamp> RegisterColorHandlers()
		{
			var instances = new List<ColorRamp>();
			Type baseType = typeof(ColorRamp);

			ExtensionManager.ProcessLoadedTypes(
				2,
				"ColorRamps",
				baseType.IsAssignableFrom,
				t => !t.IsAbstract,
				t => instances.Add(ColorRamp.LoadMap(t))
			);

			return instances;
		}

		private void OnColorHandlerSelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			var ramp = (sender as ComboBox).SelectedItem as ColorRamp;
			if (ramp != null)
			{
				PointCloudTileSource source = CurrentTileSource;
				if (source != null)
                    previewImage.Source = source.GeneratePreviewImage(ramp, CurrentColorUseStdDev, MAX_COLOR_QUALITY);
			}
		}

		private void OnStdDevCheckedStateChanged(object sender, RoutedEventArgs e)
		{
			bool? useStdDev = (sender as ToggleButton).IsChecked;
			if (useStdDev.HasValue)
			{
				PointCloudTileSource source = CurrentTileSource;
				if (source != null)
                    previewImage.Source = source.GeneratePreviewImage(CurrentColorRamp, useStdDev.Value, MAX_COLOR_QUALITY);
			}
		}

        public void LoadPreview3D()
        {
            PointCloudTileSource tileSource = CurrentTileSource;
            Jacere.Core.Geometry.Extent3D extent = tileSource.Extent;

            Model3DGroup modelGroup = new Model3DGroup();

            Model3DGroup modelSubGroup = new Model3DGroup();
            modelGroup.Children.Add(modelSubGroup);

            Model3DGroup modelStitchingGroup = new Model3DGroup();
            modelGroup.Children.Add(modelStitchingGroup);

            DirectionalLight lightSource = new DirectionalLight(System.Windows.Media.Colors.White, new Vector3D(-1, -1, -1));
            modelGroup.Children.Add(lightSource);

            ModelVisual3D model = new ModelVisual3D();
            model.Content = modelGroup;

            Jacere.Core.Geometry.Point3D centerOfMass = tileSource.CenterOfMass;
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
	}
}
