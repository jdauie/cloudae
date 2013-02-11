using System;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;

namespace Jacere.Core
{
	public class ColorMapDistinct : IColorHandler
	{
		private readonly Color[] m_colors;

		public string Name
		{
			get { return "Distinct Color Map"; }
		}

		public ColorMapDistinct()
		{
			m_colors = Enum.GetNames(typeof(KnownColor))
				.Where(item => !item.StartsWith("Control"))
				.Select(Color.FromName).ToArray();
		}

		public Color GetColor(int value)
		{
            throw new NotImplementedException();
			return m_colors[value % m_colors.Length];
		}
	}
}
