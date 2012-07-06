using System;
using System.Collections.Generic;
using System.Linq;

namespace CloudAE.Core
{
	class LAZCreator : HandlerCreatorBase
	{
		private static readonly string c_handlerName;
		private static readonly string[] c_supportedExtensions;

		static LAZCreator()
		{
			c_handlerName = "LAZ";
			c_supportedExtensions = new string[] { "laz" };
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
			FileHandlerBase inputHandler = new LAZFile(path);
			return inputHandler;
		}
	}
}
