using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CloudAE.Core
{
	public abstract class IgnorableException : Exception
	{
		protected IgnorableException(string message)
			: base(message)
		{
		}

		protected IgnorableException(string message, Exception innerException)
			: base(message, innerException)
		{
		}
	}
}
