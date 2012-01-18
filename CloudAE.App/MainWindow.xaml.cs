using System;
using System.Linq;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using System.Reflection;
using Microsoft.Win32;

using CloudAE.Core;

namespace CloudAE.App
{
	/// <summary>
	/// Interaction logic for MainWindow2.xaml
	/// </summary>
	public partial class MainWindow : Window, ISerializeStateBinary, IFactory
	{
		private static readonly ITileSourceControl[] c_controls;

		private Dictionary<string, PointCloudTileSource> m_sources;
		private PointCloudTileSource m_currentTileSource;

		private ProgressManager m_progressManager;
		private BackgroundWorker m_backgroundWorker;

		private Queue<FileHandlerBase> m_inputQueue;

		private TabItem m_tabItemOnStarted;
		private TabItem m_tabItemOnSelection;

		public PointCloudTileSource CurrentTileSource
		{
			get
			{
				return m_currentTileSource;
			}
			set
			{
				m_currentTileSource = value;
			}
		}

		static MainWindow()
		{
			List<ITileSourceControl> controls = RegisterControls();
			c_controls = controls.ToArray();
		}

		public MainWindow()
		{
			InitializeComponent();

			m_sources = new Dictionary<string, PointCloudTileSource>();

			m_inputQueue = new Queue<FileHandlerBase>();

			m_backgroundWorker = new BackgroundWorker();
			m_backgroundWorker.WorkerReportsProgress = true;
			m_backgroundWorker.WorkerSupportsCancellation = true;
			m_backgroundWorker.DoWork += OnBackgroundDoWork;
			m_backgroundWorker.ProgressChanged += OnBackgroundProgressChanged;
			m_backgroundWorker.RunWorkerCompleted += OnBackgroundRunWorkerCompleted;

			Context.LoadWindowState(this);

			foreach (ITileSourceControl control in c_controls)
			{
				Grid grid = new Grid();
				grid.Children.Add(control as UserControl);
				TabItem tabItem = new TabItem();
				tabItem.Header = control.DisplayName;
				tabItem.Content = grid;
				tabItem.Tag = control;
				tabControl.Items.Add(tabItem);
			}

			m_tabItemOnStarted = tabItemLog;
			m_tabItemOnSelection = tabControl.Items.OfType<TabItem>()
				.Where(t => t.Tag != null && typeof(Preview2D).IsAssignableFrom(t.Tag.GetType())).First();
		}

		private static List<ITileSourceControl> RegisterControls()
		{
			List<ITileSourceControl> controls = new List<ITileSourceControl>();
			Type baseType0 = typeof(ITileSourceControl);
			Type baseType1 = typeof(UserControl);

			Context.ProcessLoadedTypes(
				1,
				"Controls",
				t => baseType0.IsAssignableFrom(t) && baseType1.IsAssignableFrom(t),
				t => !t.IsAbstract,
				t => controls.Add(Activator.CreateInstance(t) as ITileSourceControl)
			);

			return controls;
		}

		private void OnBrowseButtonClick(object sender, RoutedEventArgs e)
		{
			OpenFileDialog dialog = HandlerFactory.GetOpenDialog();

			if (dialog.ShowDialog(this) == true)
			{
				string[] inputFiles = dialog.FileNames;
				foreach (string inputFile in inputFiles)
				{
					AddToQueue(inputFile);
				}

				LoadNextInput();
			}
		}

		private void AddToQueue(string inputFile)
		{
			if (File.Exists(inputFile))
			{
				FileHandlerBase inputHandler = HandlerFactory.GetInputHandler(inputFile);
				if (inputHandler != null)
				{
					m_inputQueue.Enqueue(inputHandler);
				}
			}
		}

		private void LoadNextInput()
		{
			if (m_inputQueue.Count > 0)
			{
				if (!m_backgroundWorker.IsBusy)
				{
					FileHandlerBase inputHandler = m_inputQueue.Dequeue();

					textBlockPreview.Text = inputHandler.GetPreview();

					m_tabItemOnStarted.IsSelected = true;
					m_backgroundWorker.RunWorkerAsync(inputHandler);
				}
			}
		}

		private void AppendToLog(string value)
		{
			Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate()
			{
				logViewer.AppendLine(value);
			}));
		}

		private void OnBackgroundDoWork(object sender, DoWorkEventArgs e)
		{
			FileHandlerBase inputHandler = e.Argument as FileHandlerBase;

			Action<string> logAction = new Action<string>(delegate(string value) { AppendToLog(value); });
			m_progressManager = new ProgressManager(m_backgroundWorker, e, logAction);

			ProcessingSet processingSet = new ProcessingSet(inputHandler);
			PointCloudTileSource tileSource = processingSet.Process(m_progressManager);

			if (tileSource != null)
			{
				tileSource.GeneratePreview(m_progressManager);

				e.Result = tileSource;
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
				// success
				PointCloudTileSource tileSource = e.Result as PointCloudTileSource;
				if (tileSource != null)
					AddTileSource(tileSource);

				LoadNextInput();
			}
		}

		private void OnBackgroundProgressChanged(object sender, ProgressChangedEventArgs e)
		{
			progressBar.Value = e.ProgressPercentage;
		}

		private void AddTileSource(PointCloudTileSource tileSource)
		{
			m_sources.Add(tileSource.FilePath, tileSource);

			treeView.Items.Add(tileSource);

			UpdateSelection(tileSource);
		}

		private void UpdateSelection(PointCloudTileSource tileSource)
		{
			CurrentTileSource = tileSource;
			m_tabItemOnSelection.IsSelected = true;
			UpdateTabSelection();
		}

		private void OnTreeViewDoubleClick(object sender, MouseButtonEventArgs e)
		{
			UpdateSelection((sender as TreeViewItem).Header as PointCloudTileSource);
		}

		private void OnPerformanceButtonClick(object sender, RoutedEventArgs e)
		{
			textBlockPreview.Text = PerformanceManager.GetString();
		}

		private void OnTabControlSelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (e.Source is TabControl)
			{
				foreach (ITileSourceControl control in c_controls)
					control.CurrentTileSource = null;
				
				GC.Collect();

				UpdateTabSelection();
			}
		}

		private void UpdateTabSelection()
		{
			TabItem tabItem = tabControl.SelectedItem as TabItem;
			if (tabItem != null)
			{
				ITileSourceControl control = tabItem.Tag as ITileSourceControl;
				if (control != null)
				{
					control.CurrentTileSource = CurrentTileSource;
				}
			}
		}

		private void OnWindowClosed(object sender, EventArgs e)
		{
			CurrentTileSource = null;

			Context.SaveWindowState(this);
		}

		private void OnTreeViewDrop(object sender, DragEventArgs e)
		{
			string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
			foreach (string file in files)
			{
				AddToQueue(file);
			}
			LoadNextInput();
		}

		private void OnTreeViewDragEnter(object sender, DragEventArgs e)
		{
			if (!e.Data.GetDataPresent(DataFormats.FileDrop, false))
			{
				e.Effects = DragDropEffects.None;
			}
		}

		#region ISerializeStateBinary Members

		public void Deserialize(BinaryReader reader)
		{
			Left = reader.ReadInt32();
			Top = reader.ReadInt32();
			Width = reader.ReadInt32();
			Height = reader.ReadInt32();

			if (reader.BaseStream.Position == reader.BaseStream.Length - 1)
				WindowState = System.Windows.WindowState.Maximized;
		}

		public void Serialize(BinaryWriter writer)
		{
			writer.Write((int)Left);
			writer.Write((int)Top);
			writer.Write((int)Width);
			writer.Write((int)Height);

			if (WindowState == System.Windows.WindowState.Maximized)
				writer.Write(true);
		}

		public string GetIdentifier()
		{
			//return Convert.ToBase64String(System.Text.ASCIIEncoding.ASCII.GetBytes(Name));
			return Name;
		}

		#endregion
	}
}
