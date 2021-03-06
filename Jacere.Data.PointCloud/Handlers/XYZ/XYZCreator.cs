﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace Jacere.Data.PointCloud
{
	class XYZCreator : HandlerCreatorBase
	{
		private static readonly string c_handlerName;
		private static readonly string[] c_supportedExtensions;

		static XYZCreator()
		{
			c_handlerName = "XYZ";
			c_supportedExtensions = new string[] { "txt", "csv", "xyz*" };
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
			FileHandlerBase inputHandler = new XYZFile(path);
			return inputHandler;
		}
	}
}
