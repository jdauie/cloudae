using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;

using CloudAE.Core;
using System.ComponentModel;
using System.Windows.Data;
using System.IO;

namespace CloudAE.App
{
	/// <summary>
	/// Interaction logic
	/// </summary>
	public partial class TileSourcePropertyViewer : UserControl
	{
		private PointCloudTileSource m_source;

		public TileSourcePropertyViewer()
		{
			InitializeComponent();
		}

		public PointCloudTileSource Source
		{
			get { return m_source; }
			set
			{
				m_source = value;
				DataContext = m_source;

				List<ImmutablePropertyString> properties = new List<ImmutablePropertyString>();

				if (m_source != null)
				{
					properties.Add(new ImmutablePropertyString("Info", "Name", m_source.Name));
					properties.Add(new ImmutablePropertyString("Info", "Tiled", Path.GetFileName(m_source.FilePath)));
					properties.Add(new ImmutablePropertyString("Info", "Points", string.Format("{0:0,0}", m_source.Count)));
					properties.Add(new ImmutablePropertyString("Info", "Extent", m_source.Extent));

					properties.Add(new ImmutablePropertyString("Statistics", "Mean", string.Format("{0:f}", m_source.StatisticsZ.Mean)));
					properties.Add(new ImmutablePropertyString("Statistics", "Mode", string.Format("{0:f}", m_source.StatisticsZ.ModeApproximate)));
					properties.Add(new ImmutablePropertyString("Statistics", "StdDev", string.Format("{0:f}", m_source.StatisticsZ.StdDev)));

					properties.Add(new ImmutablePropertyString("Storage", "PointDataOffset", string.Format("{0} bytes", m_source.PointDataOffset)));
					properties.Add(new ImmutablePropertyString("Storage", "PointSize", string.Format("{0} bytes", m_source.PointSizeBytes)));

					properties.Add(new ImmutablePropertyString("Quantization", "OffsetX", string.Format("{0}", m_source.Quantization.OffsetX)));
					properties.Add(new ImmutablePropertyString("Quantization", "OffsetY", string.Format("{0}", m_source.Quantization.OffsetY)));
					properties.Add(new ImmutablePropertyString("Quantization", "OffsetZ", string.Format("{0}", m_source.Quantization.OffsetZ)));
					properties.Add(new ImmutablePropertyString("Quantization", "ScaleFactorX", string.Format("{0}", m_source.Quantization.ScaleFactorX)));
					properties.Add(new ImmutablePropertyString("Quantization", "ScaleFactorY", string.Format("{0}", m_source.Quantization.ScaleFactorY)));
					properties.Add(new ImmutablePropertyString("Quantization", "ScaleFactorZ", string.Format("{0}", m_source.Quantization.ScaleFactorZ)));

					properties.Add(new ImmutablePropertyString("Tiles", "Rows", m_source.TileSet.Rows));
					properties.Add(new ImmutablePropertyString("Tiles", "Cols", m_source.TileSet.Cols));
					properties.Add(new ImmutablePropertyString("Tiles", "Tiles", string.Format("{0:0,0} ({1:0,0})", m_source.TileSet.TileCount, m_source.TileSet.ValidTileCount)));

					properties.Add(new ImmutablePropertyString("Tiles", "MinTileCount", string.Format("{0:0,0}", m_source.TileSet.Density.MinTileCount)));
					properties.Add(new ImmutablePropertyString("Tiles", "MaxTileCount", string.Format("{0:0,0}", m_source.TileSet.Density.MaxTileCount)));
					properties.Add(new ImmutablePropertyString("Tiles", "MedianTileCount", string.Format("{0:0,0}", m_source.TileSet.Density.MedianTileCount)));
					properties.Add(new ImmutablePropertyString("Tiles", "MeanTileCount", string.Format("{0:0,0}", m_source.TileSet.Density.MeanTileCount)));

					properties.Add(new ImmutablePropertyString("Tiles", "MinTileDensity", string.Format("{0:f}", m_source.TileSet.Density.MinTileDensity)));
					properties.Add(new ImmutablePropertyString("Tiles", "MaxTileDensity", string.Format("{0:f}", m_source.TileSet.Density.MaxTileDensity)));
					properties.Add(new ImmutablePropertyString("Tiles", "MedianTileDensity", string.Format("{0:f}", m_source.TileSet.Density.MedianTileDensity)));
					properties.Add(new ImmutablePropertyString("Tiles", "MeanTileDensity", string.Format("{0:f}", m_source.TileSet.Density.MeanTileDensity)));
				}

				ICollectionView m_view = CollectionViewSource.GetDefaultView(properties);
				m_view.GroupDescriptions.Add(new PropertyGroupDescription("Category"));

				PropertyGrid.ItemsSource = properties;
			}
		}
	}

	public class ImmutablePropertyString
	{
		public string Name { get; protected set; }
		public string Value { get; protected set; }
		public string Category { get; protected set; }

		public ImmutablePropertyString(string category, string name, object value)
		{
			Category = category;
			Name = name;
			Value = value != null ? value.ToString() : string.Empty;
		}
	}
}
