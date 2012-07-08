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
		private long m_size;

		public string FilePath
		{
			get { return m_path; }
			set
			{
				if (!File.Exists(value))
					throw new InvalidOperationException("FilePath cannot be set to a non-existent file.");

				m_path = value;
				m_name = Path.GetFileName(m_path);

				FileInfo fileInfo = new FileInfo(m_path);
				m_size = fileInfo.Length;
			}
		}

		public string Name
		{
			get { return m_name; }
		}

		public long Size
		{
			get { return m_size; }
		}

		protected FileHandlerBase(string path)
		{
			FilePath = path;
		}

		public abstract IPointCloudBinarySource GenerateBinarySource(ProgressManager progressManager);

		public abstract string GetPreview();
	}
}
