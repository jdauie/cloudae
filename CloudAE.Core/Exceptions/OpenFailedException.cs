using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CloudAE.Core
{
	public class OpenFailedException : IgnorableException
	{
		private string m_path;

		public OpenFailedException(string path, string message)
			: base(message)
		{
			m_path = path;
		}

		public OpenFailedException(string path, string message, Exception innerException)
			: base(message, innerException)
		{
			m_path = path;
		}

		public OpenFailedException(IFileContainer file, string message)
			: base(message)
		{
			m_path = file.FilePath;
		}

		public OpenFailedException(IFileContainer file, string message, Exception innerException)
			: base(message, innerException)
		{
			m_path = file.FilePath;
		}
	}
}
