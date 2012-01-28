using System;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

using CloudAE.Core;

namespace CloudAE.App
{
	/// <summary>
	/// Interaction logic for Profile2D.xaml
	/// </summary>
	public partial class Profile2D : UserControl, ITileSourceControl
	{
		private PointCloudTileSource m_currentTileSource;

		public string DisplayName
		{
			get { return "Profile"; }
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
				previewImageGraphicsGrid.Children.Clear();

				if (m_currentTileSource != null)
				{
					m_currentTileSource.Close();
				}

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
			
		}

		private void OnPreviewMouseUp(object sender, MouseButtonEventArgs e)
		{

		}

		private void OnPreviewMouseDown(object sender, MouseButtonEventArgs e)
		{

		}
	}
}
