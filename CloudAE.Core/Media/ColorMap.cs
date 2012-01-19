using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;

namespace CloudAE.Core
{
	public class ColorMapDistinct : IColorHandler
	{
		Color[] m_colors;

		public string Name
		{
			get { return "Distinct Color Map"; }
		}

		public ColorMapDistinct()
		{
			m_colors = Enum.GetNames(typeof(KnownColor))
				.Where(item => !item.StartsWith("Control"))
				.Select(item => Color.FromName(item)).ToArray();
		}

		public Color GetColor(uint value)
		{
			return m_colors[value % m_colors.Length];
		}
	}
}
