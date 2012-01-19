using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
//using System.Windows.Media;
using System.Drawing;

namespace CloudAE.Core
{
	/// <summary>
	/// Base ColorRamp implementation.
	/// </summary>
	public abstract class ColorRamp : IColorHandler
	{
		#region Static Members

		private static Dictionary<Type, ColorRamp> c_maps;

		/// <summary>
		/// Initializes the <see cref="ColorRamp"/> class.
		/// </summary>
		static ColorRamp()
		{
			c_maps = new Dictionary<Type, ColorRamp>();
		}

		/// <summary>
		/// Predefined Color Maps.
		/// </summary>
		public static class PredefinedColorRamps
		{
			/// <summary>Gets the elevation1 map.</summary>
			public static ColorRamp Elevation1
			{
				get { return ColorRamp.LoadMap(typeof(ColorRampElevation1)); }
			}

			/// <summary>Gets the elevation2 map.</summary>
			public static ColorRamp Elevation2
			{
				get { return ColorRamp.LoadMap(typeof(ColorRampElevation2)); }
			}

			/// <summary>Gets the bare earth map.</summary>
			public static ColorRamp BareEarth
			{
				get { return ColorRamp.LoadMap(typeof(ColorRampBareEarth)); }
			}

			/// <summary>Gets the full spectrum map.</summary>
			public static ColorRamp FullSpectrum
			{
				get { return ColorRamp.LoadMap(typeof(ColorRampFullSpectrum)); }
			}

			/// <summary>Gets the partial spectrum map.</summary>
			public static ColorRamp PartialSpectrum
			{
				get { return ColorRamp.LoadMap(typeof(ColorRampPartialSpectrum)); }
			}
		}

		/// <summary>
		/// Loads the map.
		/// </summary>
		/// <param name="type">The type.</param>
		/// <returns></returns>
		public static ColorRamp LoadMap(Type type)
		{
			ColorRamp map = null;
			if (c_maps.ContainsKey(type))
			{
				map = c_maps[type];
			}
			else if (typeof(ColorRamp).IsAssignableFrom(type))
			{
				try
				{
					map = Activator.CreateInstance(type) as ColorRamp;
				}
				catch { }

				if (map != null)
					c_maps.Add(type, map);
			}
			return map;
		}

		#endregion

		private Color[] m_map;
		private System.Windows.Media.Brush m_brush;

		public abstract string Name
		{
			get;
		}

		public System.Windows.Media.Brush HorizontalGradientBrush
		{
			get
			{
				if (m_brush == null)
				{
					var hGradient = new System.Windows.Media.LinearGradientBrush();
					hGradient.StartPoint = new System.Windows.Point(0, 0.5);
					hGradient.EndPoint = new System.Windows.Point(1, 0.5);

					for (int i = 0; i < m_map.Length; i++)
					{
						System.Windows.Media.Color color = System.Windows.Media.Color.FromRgb(m_map[i].R, m_map[i].G, m_map[i].B);
						hGradient.GradientStops.Add(new System.Windows.Media.GradientStop(color, (float)i / (m_map.Length - 1)));
					}
					
					hGradient.Freeze();
					m_brush = hGradient;
				}
				return m_brush;
			}
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="ColorRamp"/> class.
		/// </summary>
		public ColorRamp()
		{
			m_map = CreateMap();

			if (m_map == null || m_map.Length < 2)
				throw new NotImplementedException("The specified ColorRamp does not return a useful mapping.");
		}

		/// <summary>
		/// Creates the map.
		/// </summary>
		/// <returns></returns>
		protected abstract Color[] CreateMap();

		/// <summary>
		/// Gets the color.
		/// </summary>
		/// <param name="scaledValue">The value scaled from 0 to 1.</param>
		/// <returns></returns>
		public Color GetColor(double scaledValue)
		{
			if (scaledValue < 0 || scaledValue > 1)
				throw new ArgumentException("scaledValue is outside valid range", "scaledValue");

			double mapScale = scaledValue * (m_map.Length - 1);
			int mapScaleMin = (int)mapScale;
			double remainder = mapScale - mapScaleMin;

			Color color = m_map[mapScaleMin];

			if (remainder > 0)
			{
				Color minColor = m_map[mapScaleMin];
				Color maxColor = m_map[mapScaleMin + 1];

				byte R = (byte)GetValueBetween(minColor.R, maxColor.R, remainder);
				byte G = (byte)GetValueBetween(minColor.G, maxColor.G, remainder);
				byte B = (byte)GetValueBetween(minColor.B, maxColor.B, remainder);

				color = Color.FromArgb(R, G, B);
			}

			return color;
		}

		private int GetValueBetween(int start, int end, double ratio)
		{
			if (start == end)
				return start;
			else if (start < end)
				return (int)(start + (end - start) * ratio);
			else
				return (int)(end + (start - end) * (1 - ratio));
		}
	}

	/// <summary>Predefined color ramp.</summary>
	class ColorRampElevation1 : ColorRamp
	{
		/// <summary>Gets the name.</summary>
		public override string Name { get { return "Elevation 1"; } }

		/// <summary>Creates the map.</summary>
		protected override Color[] CreateMap()
		{
			return new Color[]
			{
				Color.FromArgb(175, 240, 233),
				Color.FromArgb(255, 255, 179),
				Color.FromArgb(0,   128, 64),
				Color.FromArgb(252, 186, 3),
				Color.FromArgb(128, 0,   0),
				Color.FromArgb(105, 48,  13),
				Color.FromArgb(171, 171, 171),
				Color.FromArgb(255, 252, 255)
			};
		}
	}

	/// <summary>Predefined color ramp.</summary>
	class ColorRampElevation2 : ColorRamp
	{
		/// <summary>Gets the name.</summary>
		public override string Name { get { return "Elevation 2"; } }

		/// <summary>Creates the map.</summary>
		protected override Color[] CreateMap()
		{
			return new Color[]
			{
				Color.FromArgb(118, 219, 211),
				Color.FromArgb(255, 255, 199),
				Color.FromArgb(255, 255, 199),
				Color.FromArgb(255, 255, 128),
				Color.FromArgb(217, 194, 121),
				Color.FromArgb(135, 96,  38),
				Color.FromArgb(150, 150, 181),
				Color.FromArgb(181, 150, 181),
				Color.FromArgb(25,  252, 255)
			};
		}
	}

	/// <summary>Predefined color ramp.</summary>
	class ColorRampBareEarth : ColorRamp
	{
		/// <summary>Gets the name.</summary>
		public override string Name { get { return "Bare Earth"; } }

		/// <summary>Creates the map.</summary>
		protected override Color[] CreateMap()
		{
			return new Color[]
			{
				Color.FromArgb(255, 255, 128),
				Color.FromArgb(242, 167, 46),
				Color.FromArgb(107, 0,   0)
			};
		}
	}

	/// <summary>Predefined color ramp.</summary>
	class ColorRampFullSpectrum : ColorRamp
	{
		/// <summary>Gets the name.</summary>
		public override string Name { get { return "Full Spectrum"; } }

		/// <summary>Creates the map.</summary>
		protected override Color[] CreateMap()
		{
			return new Color[]
			{
				Color.FromArgb(255, 0,   0),
				Color.FromArgb(255, 255, 0),
				Color.FromArgb(0,   255, 255),
				Color.FromArgb(0,   0,   255)
			};
		}
	}

	/// <summary>Predefined color ramp.</summary>
	class ColorRampPartialSpectrum : ColorRamp
	{
		/// <summary>Gets the name.</summary>
		public override string Name { get { return "Partial Spectrum"; } }

		/// <summary>Creates the map.</summary>
		protected override Color[] CreateMap()
		{
			return new Color[]
			{
				Color.FromArgb(115, 77,  42),
				Color.FromArgb(156, 105, 48),
				Color.FromArgb(201, 137, 52),
				Color.FromArgb(232, 193, 116),
				Color.FromArgb(255, 255, 191),
				Color.FromArgb(173, 149, 186),
				Color.FromArgb(91,  63,  176),
				Color.FromArgb(89,  39,  135),
				Color.FromArgb(81,  13,  97)
			};
		}
	}
}
