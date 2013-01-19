using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Jacere.Data.PointCloud
{
	public interface IHandlerCreator
	{
		string[] SupportedExtensions
		{
			get;
		}

		string HandlerName
		{
			get;
		}

		bool IsSupportedExtension(string extension);

		FileHandlerBase Create(string path);
	}
}
