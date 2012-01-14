using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Media.Media3D;

using CloudAE.Core;
using CloudAE.Core.Delaunay;

namespace CloudAE.App
{
	/// <summary>
	/// Interaction logic for LogViewer.xaml
	/// </summary>
	public sealed partial class LogViewer : UserControl
	{
		public LogViewer()
		{
			InitializeComponent();
		}

		public void AppendLine(string line)
		{
			textBlockLog.Text += String.Format("{0:yyyy-MM-dd HH:mm:ss}\t{1}\n", DateTime.Now, line);
		}
	}
}
