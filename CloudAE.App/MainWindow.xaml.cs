using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
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
	public partial class MainWindow : Window, ISerializeStateBinary, IFactory, IPropertyContainer
	{
		private static readonly ITileSourceControl[] c_controls;

		private static readonly PropertyState<bool> SWITCH_TO_LOG_TAB_ON_PROCESSING_START;

		private PointCloudTileSource m_currentTileSource;

		private TabItem m_tabItemOnStarted;
		private TabItem m_tabItemOnSelection;

		public PointCloudTileSource CurrentTileSource
		{
			get { return m_currentTileSource; }
			set { m_currentTileSource = value; }
		}

		static MainWindow()
		{
			SWITCH_TO_LOG_TAB_ON_PROCESSING_START = Context.RegisterOption<bool>(Context.OptionCategory.App, "SwitchToLogTabOnProcessingStart", true);

			List<ITileSourceControl> controls = RegisterControls();
			c_controls = controls.ToArray();
		}

		public MainWindow()
		{
			InitializeComponent();

			listBoxQueue.ItemsSource = Context.ProcessingQueue;

			Context.Log                       += OnLog;
			Context.ProcessingStarted         += OnProcessingStarted;
			Context.ProcessingCompleted       += OnProcessingCompleted;
			Context.ProcessingProgressChanged += OnProcessingProgressChanged;

			Context.LoadWindowState(this);

			foreach (ITileSourceControl control in c_controls)
			{
				Grid grid = new Grid();
				grid.Children.Add(control as UserControl);

				Image tabIcon = new Image();
				tabIcon.Source = control.Icon;
				tabIcon.VerticalAlignment = System.Windows.VerticalAlignment.Center;

				TextBlock text = new TextBlock();
				text.Text = control.DisplayName;
				text.Margin = new Thickness(4, 0, 0, 0);
				text.VerticalAlignment = System.Windows.VerticalAlignment.Center;

				StackPanel tabHeader = new StackPanel();
				tabHeader.Orientation = Orientation.Horizontal;
				tabHeader.Children.Add(tabIcon);
				tabHeader.Children.Add(text);

				TabItem tabItem = new TabItem();
				tabItem.Header = tabHeader;
				tabItem.Content = grid;
				tabItem.Tag = control;
				tabControl.Items.Add(tabItem);
			}

			m_tabItemOnStarted = tabItemLog;
			m_tabItemOnSelection = tabControl.Items.OfType<TabItem>()
				.Where(t => t.Tag != null && typeof(Preview2D).IsAssignableFrom(t.Tag.GetType())).FirstOrDefault();

			if(m_tabItemOnSelection == null)
				throw new Exception("Required control not available.");

			UpdateButtonStates();
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

		private void OnRemoveButtonClick(object sender, RoutedEventArgs e)
		{
			PointCloudTileSource tileSource = treeView.SelectedItem as PointCloudTileSource;
			if (tileSource != null)
				RemoveTileSource(tileSource);
		}

		private void OnRemoveAllButtonClick(object sender, RoutedEventArgs e)
		{
			List<PointCloudTileSource> sources = treeView.Items.Cast<PointCloudTileSource>().ToList();
			foreach (PointCloudTileSource tileSource in sources)
				RemoveTileSource(tileSource);
		}

		private void OnStopButtonClick(object sender, RoutedEventArgs e)
		{
			Context.ClearProcessingQueue(true);

			UpdateButtonStates();
		}

		private void OnBrowseButtonClick(object sender, RoutedEventArgs e)
		{
			OpenFileDialog dialog = HandlerFactory.GetOpenDialog();

			if (dialog.ShowDialog(this) == true)
			{
				string[] inputFiles = dialog.FileNames;
				Context.AddToProcessingQueue(inputFiles);
			}
		}

		#region Context Event Handlers

		private void OnLog(string value)
		{
			Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate()
			{
				logViewer.AppendLine(value);
			}));
		}

		private void OnProcessingStarted(FileHandlerBase inputHandler)
		{
			textBlockPreview.Text = inputHandler.GetPreview();

			if (SWITCH_TO_LOG_TAB_ON_PROCESSING_START.Value)
				m_tabItemOnStarted.IsSelected = true;

			UpdateButtonStates();
		}

		private void OnProcessingCompleted(PointCloudTileSource tileSource)
		{
			// this could be determined with additional event info
			textBlockPreview.Text = "[Queue empty]";

			if (tileSource != null)
				AddTileSource(tileSource);

			UpdateButtonStates();
		}

		private void OnProcessingProgressChanged(int progressPercentage)
		{
			progressBar.Value = progressPercentage;
		}

		#endregion

		private void AddTileSource(PointCloudTileSource tileSource)
		{
			treeView.Items.Add(tileSource);
			TreeViewItem treeViewItem = treeView.ItemContainerGenerator.ContainerFromItem(tileSource) as TreeViewItem;
			treeViewItem.BringIntoView();
			treeViewItem.IsSelected = true;

			UpdateSelection(tileSource);

			UpdateButtonStates();
		}

		private void RemoveTileSource(PointCloudTileSource tileSource)
		{
			if (tileSource != null)
			{
				PointCloudTileSource selectedTileSource = treeView.SelectedItem as PointCloudTileSource;

				if (selectedTileSource == tileSource)
					UpdateSelection(null);

				Context.Remove(tileSource);

				// this should probably be a callback operation
				treeView.Items.Remove(tileSource);

				UpdateButtonStates();
			}
		}

		private void UpdateButtonStates()
		{
			itemRemoveAll.IsEnabled = Context.HasTileSources;
			itemRemove.IsEnabled = (treeView.SelectedItem != null);
			itemStop.IsEnabled = !Context.IsProcessingQueueEmpty || Context.IsProcessing;

			buttonRemove.IsEnabled = itemRemove.IsEnabled;
			buttonStop.IsEnabled = itemStop.IsEnabled;
		}

		private void UpdateSelection(PointCloudTileSource tileSource)
		{
			CurrentTileSource = tileSource;

			if (tileSource != null)
				m_tabItemOnSelection.IsSelected = true;
			else
				m_tabItemOnStarted.IsSelected = true;

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
			bool enableTileControls = (CurrentTileSource != null);

			foreach (TabItem tabItem in tabControl.Items)
			{
				ITileSourceControl control = tabItem.Tag as ITileSourceControl;
				if (control != null)
				{
					tabItem.IsEnabled = enableTileControls;

					if (tabItem.IsSelected)
						control.CurrentTileSource = CurrentTileSource;
				}
			}
		}

		protected override void OnClosed(EventArgs e)
		{
			CurrentTileSource = null;

			Context.SaveWindowState(this);

			Context.Log -= OnLog;
			Context.ProcessingStarted -= OnProcessingStarted;
			Context.ProcessingCompleted -= OnProcessingCompleted;
			Context.ProcessingProgressChanged -= OnProcessingProgressChanged;

			base.OnClosed(e);
		}

		private void OnTreeViewDrop(object sender, DragEventArgs e)
		{
			string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
			Context.AddToProcessingQueue(files);
		}

		private void OnTreeViewDragEnter(object sender, DragEventArgs e)
		{
			if (!e.Data.GetDataPresent(DataFormats.FileDrop, false))
				e.Effects = DragDropEffects.None;
		}

		#region ISerializeStateBinary Members

		public void Deserialize(BinaryReader reader)
		{
			this.DeserializeState(reader);
		}

		public void Serialize(BinaryWriter writer)
		{
			this.SerializeState(writer);
		}

		public string GetIdentifier()
		{
			return Name;
		}

		#endregion
	}
}
