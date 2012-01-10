using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Diagnostics;
using CloudAE.Core.Geometry;
using CloudAE.Core.Compression;

namespace CloudAE.Core
{
	class LASCreator : HandlerCreatorBase
	{
		private static readonly string c_handlerName;
		private static readonly string[] c_supportedExtensions;

		static LASCreator()
		{
			c_handlerName = "LAS";
			c_supportedExtensions = new string[] { "las" };
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
			FileHandlerBase inputHandler = new LASFile(path);
			return inputHandler;
		}
	}
}
