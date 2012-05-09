using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media.Imaging;

namespace CloudAE.Core
{
	public class PreviewImage
	{
		public readonly BitmapSource Image;
		public readonly IColorHandler ColorHandler;
		public readonly bool UseStdDevStretch;
		public readonly int Quality;

		public PreviewImage(BitmapSource image, IColorHandler colorHandler, bool useStdDevStretch, int quality)
		{
			Image = image;
			ColorHandler = colorHandler;
			UseStdDevStretch = useStdDevStretch;
			Quality = quality;
		}
	}
}
