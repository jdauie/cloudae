using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CloudAE.Core
{
	public abstract class IgnorableException : Exception
	{
		public IgnorableException(string message)
			: base(message)
		{
		}

		public IgnorableException(string message, Exception innerException)
			: base(message, innerException)
		{
		}
	}
}
