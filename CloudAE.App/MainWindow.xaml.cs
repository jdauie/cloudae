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
			c_controls = controls.OrderBy(c => c.Index).ToArray();
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
				UserControl userControl = control as UserControl;
				if (!userControl.IsEnabled)
					continue;

				Grid grid = new Grid();
				grid.Children.Add(userControl);

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
				.FirstOrDefault(t => t.Tag != null && typeof(Preview2D).IsInstanceOfType(t.Tag));

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
			PointCloudTileSource tileSource = ContentList.SelectedItem as PointCloudTileSource;
			if (tileSource != null)
				RemoveTileSource(tileSource);
		}

		private void OnRemoveAllButtonClick(object sender, RoutedEventArgs e)
		{
			List<PointCloudTileSource> sources = ContentList.Items.Cast<PointCloudTileSource>().ToList();
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
			Dispatcher.Invoke(DispatcherPriority.Normal, new Action(() => logViewer.AppendLine(value)));
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
			int index = ContentList.Items.Add(tileSource);
			ContentList.SelectedIndex = index;
			ContentList.ScrollIntoView(tileSource);

			UpdateSelection(tileSource);

			UpdateButtonStates();
		}

		private void RemoveTileSource(PointCloudTileSource tileSource)
		{
			if (tileSource != null)
			{
				if (CurrentTileSource == tileSource)
					UpdateSelection(null);

				Context.Remove(tileSource);

				ContentList.Items.Remove(tileSource);

				UpdateButtonStates();
			}
		}

		private void UpdateButtonStates()
		{
			itemRemoveAll.IsEnabled = Context.HasTileSources;
			itemRemove.IsEnabled = (ContentList.SelectedItem != null);
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

		private void OnListSelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			UpdateButtonStates();

			propertyViewer.Source = ContentList.SelectedItem as PointCloudTileSource;
		}

		private void OnListDoubleClick(object sender, MouseButtonEventArgs e)
		{
			UpdateSelection((sender as ListBoxItem).Content as PointCloudTileSource);
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

		private void OnClearCacheButtonClick(object sender, RoutedEventArgs e)
		{
			Cache.Clear();
		}

		private void OnExitButtonClick(object sender, RoutedEventArgs e)
		{
			Close();
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

		private void OnListDrop(object sender, DragEventArgs e)
		{
			string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
			Context.AddToProcessingQueue(files);
		}

		private void OnListDragEnter(object sender, DragEventArgs e)
		{
			if (!e.Data.GetDataPresent(DataFormats.FileDrop, false))
				e.Effects = DragDropEffects.None;
		}

		#region ISerializeStateBinary Members

		public void Deserialize(BinaryReader reader)
		{
			if (this.DeserializeState(reader))
			{
				if (reader.BaseStream.Length - reader.BaseStream.Position == 2 * sizeof(int))
				{
					GridColumnLeft.Width = new GridLength(reader.ReadInt32());
					GridColumnRight.Width = new GridLength(reader.ReadInt32());
				}
			}
		}

		public void Serialize(BinaryWriter writer)
		{
			this.SerializeState(writer);

			writer.Write((int)GridColumnLeft.ActualWidth);
			writer.Write((int)GridColumnRight.ActualWidth);
		}

		public string GetIdentifier()
		{
			return Name;
		}

		#endregion
	}
}
