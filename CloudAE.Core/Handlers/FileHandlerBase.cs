using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CloudAE.Core
{
	public abstract class FileHandlerBase
	{
		private string m_path;

		public string FilePath
		{
			get { return m_path; }
		}

		public abstract string[] SupportedExtensions
		{
			get;
		}

		protected FileHandlerBase(string path)
		{
			m_path = path;
		}

		public abstract PointCloudBinarySource GenerateBinarySource(ProgressManager progressManager);

		public abstract string GetPreview();
	}
}
