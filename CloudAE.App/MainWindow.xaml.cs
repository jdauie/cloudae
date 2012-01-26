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
			//treeView.SelectedItem
		}

		private void OnRemoveAllButtonClick(object sender, RoutedEventArgs e)
		{
			//PointCloudTileSource[] sources = m_sources.Values.ToArray();
			//foreach (PointCloudTileSource source in sources)
			//    RemoveTileSource(source);
		}

		private void OnStopButtonClick(object sender, RoutedEventArgs e)
		{
			//m_inputQueue.Clear();
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
		}

		private void OnProcessingCompleted(PointCloudTileSource tileSource)
		{
			// this could be determined with additional event info
			textBlockPreview.Text = "[Queue empty]";

			if (tileSource != null)
				AddTileSource(tileSource);
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
			UpdateSelection(null);

			//m_sources.Remove(tileSource.FilePath);

			treeView.Items.Remove(tileSource);

			UpdateButtonStates();
		}

		private void UpdateButtonStates()
		{
			//itemRemoveAll.IsEnabled = (m_sources.Count > 0);
			itemRemove.IsEnabled = (treeView.SelectedItem != null);
			//itemStop.IsEnabled = (m_inputQueue.Count > 0);

			buttonRemove.IsEnabled = itemRemove.IsEnabled;
			buttonStop.IsEnabled = itemRemove.IsEnabled;
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

			Context.Log                       -= OnLog;
			Context.ProcessingStarted         -= OnProcessingStarted;
			Context.ProcessingCompleted       -= OnProcessingCompleted;
			Context.ProcessingProgressChanged -= OnProcessingProgressChanged;
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
