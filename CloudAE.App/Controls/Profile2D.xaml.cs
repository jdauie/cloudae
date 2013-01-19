using System;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

using CloudAE.Core;
using System.Windows;
using System.Windows.Shapes;

namespace CloudAE.App
{
	/// <summary>
	/// Interaction logic for Profile2D.xaml
	/// </summary>
	public partial class Profile2D : UserControl, ITileSourceControl
	{
		private PointCloudTileSource m_currentTileSource;

		private bool m_isSelecting;
		private bool m_hasSelection;
		private System.Windows.Point m_point0;
		private System.Windows.Point m_point1;
		private Line m_profileLine;

		public string DisplayName
		{
			get { return "Profile"; }
		}

		public int Index
		{
			get { return 3; }
		}

		public ImageSource Icon
		{
			get { return new BitmapImage(new Uri("pack://application:,,,/CloudAE.App;component/Icons/shape_rotate_clockwise.png")); }
		}

		public PointCloudTileSource CurrentTileSource
		{
			get
			{
				return m_currentTileSource;
			}
			set
			{
				CancelSelection();

				m_currentTileSource = value;

				if (m_currentTileSource != null)
				{
					previewImage.Source = m_currentTileSource.Preview.Image;
				}
				else
				{
					previewImage.Source = null;
				}
			}
		}

		public Profile2D()
		{
			InitializeComponent();
		}

		private void OnPreviewMouseMove(object sender, MouseEventArgs e)
		{
			if (m_isSelecting)
			{
				m_point1 = GetPointRatio(previewImage, e);
				RedrawLine();
			}
		}

		private void OnPreviewMouseDown(object sender, MouseButtonEventArgs e)
		{
			if (m_isSelecting)
			{
				if (e.ChangedButton != MouseButton.Left)
				{
					CancelSelection();
					return;
				}

				m_isSelecting = false;
				m_hasSelection = true;

				RedrawLine();

				// get points in region and display profile
				Jacere.Core.Geometry.Point3D[] pointsInRegion = CurrentTileSource.GetPointsNearLine(m_point0, m_point1, 1.0, true);
			}
			else
			{
				if (e.ChangedButton == MouseButton.Left)
				{
					m_point0 = GetPointRatio(previewImage, e);

					ClearPreviewGraphics();

					m_isSelecting = true;
					m_hasSelection = false;
				}
			}
		}

		private void CancelSelection()
		{
			ClearPreviewGraphics();

			m_isSelecting = false;
			m_hasSelection = false;
		}

		private void ClearPreviewGraphics()
		{
			previewImageGraphicsGrid.Children.Clear();
			m_profileLine = null;
		}

		private static System.Windows.Point GetPointRatio(Image image, MouseEventArgs e)
		{
			System.Windows.Point point = e.GetPosition(image);
			System.Windows.Point pointRatio = new System.Windows.Point(point.X / image.ActualWidth, 1 - (point.Y / image.ActualHeight));
			return pointRatio;
		}

		private void OnPreviewSizeChanged(object sender, SizeChangedEventArgs e)
		{
			double imageLocationX = (previewImageGrid.ActualWidth - previewImage.ActualWidth) / 2;
			double imageLocationY = (previewImageGrid.ActualHeight - previewImage.ActualHeight) / 2;

			previewImageGraphicsGrid.Margin = new Thickness(imageLocationX, imageLocationY, 0, 0);

			RedrawLine();
		}

		private void RedrawLine()
		{
			if (m_profileLine == null)
			{
				if (!m_hasSelection && !m_isSelecting)
					return;

				m_profileLine = new Line();
				m_profileLine.Stroke = System.Windows.Media.Brushes.Black;
				m_profileLine.HorizontalAlignment = System.Windows.HorizontalAlignment.Left;
				m_profileLine.VerticalAlignment = System.Windows.VerticalAlignment.Top;
				m_profileLine.StrokeThickness = 2;
				previewImageGraphicsGrid.Children.Add(m_profileLine);
			}

			m_profileLine.X1 = previewImage.ActualWidth * m_point0.X;
			m_profileLine.X2 = previewImage.ActualWidth * m_point1.X;
			m_profileLine.Y1 = previewImage.ActualHeight * (1.0 - m_point0.Y);
			m_profileLine.Y2 = previewImage.ActualHeight * (1.0 - m_point1.Y);
		}
	}
}
