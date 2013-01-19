using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CloudAE.Core
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
