using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Media3D;
using System.Windows.Markup;
using System.Collections.Generic;
using System.Timers;

namespace CloudAE.Core
{
	public class FlyMotionController
	{
		private FrameworkElement m_eventSource;
		private Point m_previousPosition2D;
		private Vector3D _previousPosition3D = new Vector3D(0, 0, 1);

		private Transform3DGroup m_transform;
		private ScaleTransform3D m_scale = new ScaleTransform3D();
		private AxisAngleRotation3D m_rotation = new AxisAngleRotation3D();

		private Key m_activeZoomKey;
		private Key m_activePanKey;

		private Timer m_timer;

		public FlyMotionController()
		{
			m_timer = new Timer(10);
			m_timer.Elapsed += OnTimerElapsed;

			m_transform = new Transform3DGroup();
			m_transform.Children.Add(m_scale);
			m_transform.Children.Add(new RotateTransform3D(m_rotation));
		}

		/// <summary>
		/// A transform to move the camera or scene to the current orientation and scale.
		/// </summary>
		public Transform3D Transform
		{
			get { return m_transform; }
		}

		#region Event Handling

		/// <summary>
		/// The FrameworkElement source for mouse and keyboard events.
		/// </summary>
		public FrameworkElement EventSource
		{
			get
			{
				return m_eventSource;
			}
			set
			{
				if (m_eventSource != null)
				{
					m_eventSource.MouseDown -= this.OnMouseDown;
					m_eventSource.MouseUp   -= this.OnMouseUp;
					m_eventSource.MouseMove -= this.OnMouseMove;
					m_eventSource.KeyDown   -= this.OnKeyDown;
					m_eventSource.KeyUp     -= this.OnKeyUp;
				}

				m_eventSource = value;

				m_eventSource.MouseDown += this.OnMouseDown;
				m_eventSource.MouseUp   += this.OnMouseUp;
				m_eventSource.MouseMove += this.OnMouseMove;
				m_eventSource.KeyDown   += this.OnKeyDown;
				m_eventSource.KeyUp     += this.OnKeyUp;
			}
		}

		private void OnMouseDown(object sender, MouseEventArgs e)
		{
			Mouse.Capture(EventSource, CaptureMode.Element);

			m_previousPosition2D = e.GetPosition(EventSource);

			if (e.LeftButton == MouseButtonState.Pressed)
			{
				m_timer.Start();
			}
		}

		private void OnMouseUp(object sender, MouseEventArgs e)
		{
			Mouse.Capture(EventSource, CaptureMode.None);
		}

		private void OnMouseMove(object sender, MouseEventArgs e)
		{
			Point currentPosition = e.GetPosition(EventSource);

			

			m_previousPosition2D = currentPosition;
		}

		private void OnKeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Up || e.Key == Key.Down)
			{
				m_activeZoomKey = e.Key;
			}
		}

		private void OnKeyUp(object sender, KeyEventArgs e)
		{
			if (e.Key == m_activeZoomKey)
			{
				m_activeZoomKey = Key.None;
			}
		}

		private void OnTimerElapsed(object sender, ElapsedEventArgs e)
		{
			Point center = new Point(m_eventSource.ActualWidth / 2, m_eventSource.ActualHeight / 2);
			Vector centerToCurrent = m_previousPosition2D - center;

			// direction for rotation
			// distance for speed

			//Point3D pseudoCamera = new Point3D();


			m_scale.Dispatcher.Invoke(
				System.Windows.Threading.DispatcherPriority.Normal,
				new Action(
				delegate()
				{

					if (m_activeZoomKey != Key.None)
					{
						double delta = 1.0;
						if (m_activeZoomKey == Key.Up)
							delta *= -1;

						Zoom(delta);
					}
				}
			));
		}

		#endregion

		//private void Track(Point currentPosition)
		//{
		//    Vector3D currentPosition3D = ProjectToTrackball(
		//        EventSource.ActualWidth, EventSource.ActualHeight, currentPosition);

		//    Vector3D axis = Vector3D.CrossProduct(_previousPosition3D, currentPosition3D);
		//    double angle = Vector3D.AngleBetween(_previousPosition3D, currentPosition3D);
		//    Quaternion delta = new Quaternion(axis, -angle);

		//    // Get the current orientantion from the RotateTransform3D
		//    AxisAngleRotation3D r = m_rotation;
		//    Quaternion q = new Quaternion(m_rotation.Axis, m_rotation.Angle);

		//    // Compose the delta with the previous orientation
		//    q *= delta;

		//    // Write the new orientation back to the Rotation3D
		//    m_rotation.Axis = q.Axis;
		//    m_rotation.Angle = q.Angle;

		//    _previousPosition3D = currentPosition3D;
		//}

		//private Vector3D ProjectToTrackball(double width, double height, Point point)
		//{
		//    double x = point.X / (width / 2);    // Scale so bounds map to [0,0] - [2,2]
		//    double y = point.Y / (height / 2);

		//    x = x - 1;                           // Translate 0,0 to the center
		//    y = 1 - y;                           // Flip so +Y is up instead of down

		//    double z2 = 1 - x * x - y * y;       // z^2 = 1 - x^2 - y^2
		//    double z = z2 > 0 ? Math.Sqrt(z2) : 0;

		//    return new Vector3D(x, y, z);
		//}

		private void Zoom(double delta)
		{
			//double yDelta = currentPosition.Y - m_previousPosition2D.Y;

			double scale = Math.Exp(delta / 100);    // e^(yDelta/100) is fairly arbitrary.

			m_scale.ScaleX *= scale;
			m_scale.ScaleY *= scale;
			m_scale.ScaleZ *= scale;
		}
	}
}
