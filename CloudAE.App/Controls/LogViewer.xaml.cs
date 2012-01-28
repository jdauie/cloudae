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
		private Brush m_foregroundBrushTime;
		private Brush m_foregroundBrushHighlight;

		public LogViewer()
		{
			InitializeComponent();

			m_foregroundBrushTime = new SolidColorBrush(Colors.LightGray);
			m_foregroundBrushTime.Freeze();

			m_foregroundBrushHighlight = new SolidColorBrush(Colors.Firebrick);
			m_foregroundBrushHighlight.Freeze();
		}

		public void AppendLine(string line)
		{
			int i = 0;
			while (i < line.Length)
			{
				if (char.IsLetterOrDigit(line[i]))
					break;
				++i;
			}

			Run run0 = new Run(String.Format("{0:yyyy-MM-dd HH:mm:ss}  ", DateTime.Now));
			run0.Foreground = m_foregroundBrushTime;
			run0.FontFamily = new System.Windows.Media.FontFamily("Consolas");

			Run run1 = new Run(line.Substring(0, i));
			run1.Foreground = m_foregroundBrushHighlight;

			Run run2 = new Run(line.Substring(i, line.Length - i));

			richTextBoxParagraph.Inlines.Add(run0);
			richTextBoxParagraph.Inlines.Add(run1);
			richTextBoxParagraph.Inlines.Add(run2);
			richTextBoxParagraph.Inlines.Add(new LineBreak());

			scrollViewer.ScrollToBottom();
		}
	}
}
