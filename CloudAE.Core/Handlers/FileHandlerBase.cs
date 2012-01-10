using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace CloudAE.Core
{
	public abstract class FileHandlerBase
	{
		private string m_path;

		public string FilePath
		{
			get { return m_path; }
			set
			{
				if (!File.Exists(value))
					throw new InvalidOperationException("FilePath cannot be set to a non-existent file.");

				m_path = value;
			}
		}

		protected FileHandlerBase(string path)
		{
			m_path = path;
		}

		public abstract PointCloudBinarySource GenerateBinarySource(ProgressManager progressManager);

		public abstract string GetPreview();
	}
}
