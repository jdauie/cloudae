using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using Microsoft.Win32;

using CloudAE.Core;

namespace CloudAE.App
{
	/// <summary>
	/// Interaction logic for MainWindow2.xaml
	/// </summary>
	public partial class MainWindow : Window, ISerializeStateBinary
	{
		private Dictionary<string, PointCloudTileSource> m_sources;
		private PointCloudTileSource m_currentTileSource;

		private ProgressManager m_progressManager;
		private BackgroundWorker m_backgroundWorker;

		private Queue<FileHandlerBase> m_inputQueue;

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

					tabItemLog.IsSelected = true;
					m_backgroundWorker.RunWorkerAsync(inputHandler);
				}
			}
		}

		private void AppendToLog(string value)
		{
			Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate()
			{
				textBlockLog.Text += String.Format("{0:yyyy-MM-dd HH:mm:ss}\t{1}\n", DateTime.Now, value);
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
				tileSource.GeneratePreview(1000, m_progressManager);

				//tileSource.GeneratePreviewGrid(700, m_progressManager);

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
			tabItem2D.IsSelected = true;
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
				preview3D.CurrentTileSource = null;
				preview2D.CurrentTileSource = null;
				GC.Collect();

				UpdateTabSelection();
			}
		}

		private void UpdateTabSelection()
		{
			if (tabItem3D.IsSelected)
			{
				preview3D.CurrentTileSource = m_currentTileSource;
			}
			else if (tabItem2D.IsSelected)
			{
				preview2D.CurrentTileSource = m_currentTileSource;
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
