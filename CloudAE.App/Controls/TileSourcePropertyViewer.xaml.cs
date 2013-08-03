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

				var properties = new List<ImmutablePropertyString>();

				if (m_source != null)
				{
					using (var group = new PropertyGroup(properties, "Info"))
					{
						group.Add("Name", m_source.Name);
						group.Add("Tiled", Path.GetFileName(m_source.FilePath));
						group.Add("Points", string.Format("{0:0,0}", m_source.Count));
						group.Add("Extent", m_source.Extent);
					}

					using (var group = new PropertyGroup(properties, "Statistics"))
					{
						group.Add("Mean", string.Format("{0:f}", m_source.StatisticsZ.Mean));
						group.Add("Mode", string.Format("{0:f}", m_source.StatisticsZ.ModeApproximate));
						group.Add("StdDev", string.Format("{0:f}", m_source.StatisticsZ.StdDev));
					}

					using (var group = new PropertyGroup(properties, "Storage"))
					{
						group.Add("PointDataOffset", string.Format("{0} bytes", m_source.PointDataOffset));
						group.Add("PointSize", string.Format("{0} bytes", m_source.PointSizeBytes));
					}

					using (var group = new PropertyGroup(properties, "Quantization"))
					{
						group.Add("OffsetX", string.Format("{0}", m_source.Quantization.OffsetX));
						group.Add("OffsetY", string.Format("{0}", m_source.Quantization.OffsetY));
						group.Add("OffsetZ", string.Format("{0}", m_source.Quantization.OffsetZ));
						group.Add("ScaleFactorX", string.Format("{0}", m_source.Quantization.ScaleFactorX));
						group.Add("ScaleFactorY", string.Format("{0}", m_source.Quantization.ScaleFactorY));
						group.Add("ScaleFactorZ", string.Format("{0}", m_source.Quantization.ScaleFactorZ));
					}

					using (var group = new PropertyGroup(properties, "Tiles"))
					{
						group.Add("Rows", m_source.TileSet.Rows);
						group.Add("Cols", m_source.TileSet.Cols);
						group.Add("Tiles", string.Format("{0:0,0} ({1:0,0})", m_source.TileSet.TileCount, m_source.TileSet.ValidTileCount));

						group.Add("MinTileCount", string.Format("{0:0,0}", m_source.TileSet.Density.MinTileCount));
						group.Add("MaxTileCount", string.Format("{0:0,0}", m_source.TileSet.Density.MaxTileCount));
						group.Add("MedianTileCount", string.Format("{0:0,0}", m_source.TileSet.Density.MedianTileCount));
						group.Add("MeanTileCount", string.Format("{0:0,0}", m_source.TileSet.Density.MeanTileCount));

						group.Add("MinTileDensity", string.Format("{0:f}", m_source.TileSet.Density.MinTileDensity));
						group.Add("MaxTileDensity", string.Format("{0:f}", m_source.TileSet.Density.MaxTileDensity));
						group.Add("MedianTileDensity", string.Format("{0:f}", m_source.TileSet.Density.MedianTileDensity));
						group.Add("MeanTileDensity", string.Format("{0:f}", m_source.TileSet.Density.MeanTileDensity));
					}
				}

				var view = CollectionViewSource.GetDefaultView(properties);
				view.GroupDescriptions.Add(new PropertyGroupDescription("Category"));

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

	public class PropertyGroup : IDisposable
	{
		private readonly List<ImmutablePropertyString> m_list;
		private readonly string m_category;

		public PropertyGroup(List<ImmutablePropertyString> list, string category)
		{
			m_list = list;
			m_category = category;
		}

		public void Add(string name, object value)
		{
			m_list.Add(new ImmutablePropertyString(m_category, name, value.ToString()));
		}

		#region IDisposable Implementation

		public void Dispose()
		{
		}

		#endregion
	}
}
