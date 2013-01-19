using System;
using System.Collections.Generic;
using System.Linq;

namespace Jacere.Data.PointCloud
{
	class LASCreator : HandlerCreatorBase
	{
		private static readonly string c_handlerName;
		private static readonly string[] c_supportedExtensions;

		static LASCreator()
		{
			c_handlerName = "LAS";
			c_supportedExtensions = new string[] { "las", "lasgroup" };
		}

		public override string[] SupportedExtensions
		{
			get { return c_supportedExtensions; }
		}

		public override string HandlerName
		{
			get { return c_handlerName; }
		}

		public override FileHandlerBase Create(string path)
		{
			FileHandlerBase inputHandler;
			if (path.EndsWith("group"))
				inputHandler = new LASComposite(path);
			else
				inputHandler = new LASFile(path);

			return inputHandler;
		}
	}
}
