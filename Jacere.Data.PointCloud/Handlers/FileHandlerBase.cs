using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Jacere.Core;

namespace Jacere.Data.PointCloud
{
	public interface IFileContainer
	{
		string FilePath { get; }
	}

	public abstract class FileHandlerBase : IFileContainer
	{
		private string m_path;
		private string m_name;
		private long m_size;

		public string FilePath
		{
			get { return m_path; }
			set
			{
				//if (!File.Exists(value))
				//	throw new InvalidOperationException("FilePath cannot be set to a non-existent file.");

				m_path = value;
				m_name = Path.GetFileName(m_path);

				m_size = Size;
			}
		}

		public string Name
		{
			get { return m_name; }
		}

		public long Size
		{
			get
			{
				if (m_size == 0)
				{
					FileInfo fileInfo = new FileInfo(m_path);
					if (fileInfo.Exists)
						m_size = fileInfo.Length;
				}
				return m_size;
			}
		}

		public bool Exists
		{
			get { return File.Exists(FilePath); }
		}

		public virtual bool IsValid
		{
			get { return Exists; }
		}

		protected FileHandlerBase(string path)
		{
			FilePath = path;
		}

		public abstract IPointCloudBinarySource GenerateBinarySource(ProgressManager progressManager);

		public abstract string GetPreview();
	}
}
