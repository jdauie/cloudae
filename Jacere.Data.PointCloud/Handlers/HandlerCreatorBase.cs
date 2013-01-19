using System;
using System.Collections.Generic;
using System.Linq;

namespace CloudAE.Core
{
	abstract class HandlerCreatorBase : IHandlerCreator
	{
		#region IHandlerCreator Members

		public abstract string[] SupportedExtensions
		{
			get;
		}

		public abstract string HandlerName
		{
			get;
		}

		public abstract FileHandlerBase Create(string path);

		#endregion

		public bool IsSupportedExtension(string extension)
		{
			extension = extension.ToLower();
			if (extension.StartsWith("."))
				extension = extension.Substring(1);

			string[] supportedPatterns = SupportedExtensions;

			foreach (string pattern in supportedPatterns)
			{
				if (pattern == extension)
					return true;

				// this is flaky
				if (pattern[pattern.Length - 1] == '*' && pattern.Substring(0, pattern.Length - 1) == extension.Substring(0, pattern.Length - 1))
					return true;
			}

			return false;
		}
	}
}
