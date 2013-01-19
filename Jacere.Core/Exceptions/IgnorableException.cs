using System;

namespace Jacere.Core
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
