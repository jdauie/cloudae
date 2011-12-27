using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Diagnostics;

namespace CloudAE.Core
{
	public class HandlerFactory
	{
		public FileHandlerBase GetInputHandler(string path)
		{
			// this should go somewhere on startup
			if (!BitConverter.IsLittleEndian)
			{
				throw new NotSupportedException();
			}

			FileHandlerBase inputHandler = null;
			string extension = Path.GetExtension(path).ToLower();

			switch (extension)
			{
				case ".las":
					inputHandler = new LASFile(path);
					break;

				case ".xyz":
				case ".txt":
					inputHandler = new XYZFile(path);
					break;
			}

			return inputHandler;
		}
	}
}
