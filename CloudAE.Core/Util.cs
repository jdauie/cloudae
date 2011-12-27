using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using System.Windows.Media.Imaging;
using System.Windows;
using System.Drawing;

namespace CloudAE.Core
{
	public class Util
	{
		[DllImport("gdi32")]
		static extern int DeleteObject(IntPtr o);

		public static BitmapSource LoadBitmap(Bitmap source)
		{
			IntPtr ip = source.GetHbitmap();
			BitmapSource bs = null;
			try
			{
				bs = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(
					ip, 
					IntPtr.Zero, 
					Int32Rect.Empty, 
					System.Windows.Media.Imaging.BitmapSizeOptions.FromEmptyOptions()
				);
			}
			finally
			{
				DeleteObject(ip);
			}

			return bs;
		}
	}
}
