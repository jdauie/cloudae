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
		private ScaleTransform3D _scale = new ScaleTransform3D();
		private AxisAngleRotation3D _rotation = new AxisAngleRotation3D();

		private Key m_activeZoomKey;
		private Key m_activePanKey;

		private Timer m_timer;

		public FlyMotionController()
		{
			m_timer = new Timer(10);
			m_timer.Elapsed += new ElapsedEventHandler(OnTimerElapsed);

			m_transform = new Transform3DGroup();
			m_transform.Children.Add(_scale);
			m_transform.Children.Add(new RotateTransform3D(_rotation));
		}

		/// <summary>
		///     A transform to move the camera or scene to the trackball's
		///     current orientation and scale.
		/// </summary>
		public Transform3D Transform
		{
			get { return m_transform; }
		}

		#region Event Handling

		/// <summary>
		///     The FrameworkElement we listen to for mouse events.
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
			throw new NotImplementedException();
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
		//    AxisAngleRotation3D r = _rotation;
		//    Quaternion q = new Quaternion(_rotation.Axis, _rotation.Angle);

		//    // Compose the delta with the previous orientation
		//    q *= delta;

		//    // Write the new orientation back to the Rotation3D
		//    _rotation.Axis = q.Axis;
		//    _rotation.Angle = q.Angle;

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

		private void Zoom(Point currentPosition)
		{
			double yDelta = currentPosition.Y - m_previousPosition2D.Y;

			double scale = Math.Exp(yDelta / 100);    // e^(yDelta/100) is fairly arbitrary.

			_scale.ScaleX *= scale;
			_scale.ScaleY *= scale;
			_scale.ScaleZ *= scale;
		}
	}
}
