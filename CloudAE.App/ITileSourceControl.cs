using System.Windows.Media;

using CloudAE.Core;

namespace CloudAE.App
{
	interface ITileSourceControl
	{
		string DisplayName
		{
			get;
		}

		ImageSource Icon
		{
			get;
		}

		int Index
		{
			get;
		}

		PointCloudTileSource CurrentTileSource
		{
			get;
			set;
		}
	}
}
