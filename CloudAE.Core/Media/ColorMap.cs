using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;

namespace CloudAE.Core
{
	public interface IColorHandler
	{

	}

	public class ColorMapDistinct : IColorHandler
	{
		Color[] m_colors;

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
