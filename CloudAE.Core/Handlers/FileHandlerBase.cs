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
		private string m_name;

		public string FilePath
		{
			get { return m_path; }
			set
			{
				if (!File.Exists(value))
					throw new InvalidOperationException("FilePath cannot be set to a non-existent file.");

				m_path = value;
				m_name = Path.GetFileName(m_path);
			}
		}

		public string Name
		{
			get { return m_name; }
		}

		protected FileHandlerBase(string path)
		{
			FilePath = path;
		}

		public abstract PointCloudBinarySource GenerateBinarySource(ProgressManager progressManager);

		public abstract string GetPreview();
	}
}
