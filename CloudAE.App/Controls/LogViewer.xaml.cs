using System;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace CloudAE.App
{
	/// <summary>
	/// Interaction logic for LogViewer.xaml
	/// </summary>
	public sealed partial class LogViewer : UserControl
	{
		private readonly Brush m_foregroundBrushTime;
		private readonly Brush m_foregroundBrushHighlight;

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
