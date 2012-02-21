using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace CloudAE.Core
{
	public abstract class PointCloudSource
	{
		private readonly string m_filePath;
		
		private readonly string m_name;

		public virtual string FilePath
		{
			get { return m_filePath; }
		}

		public virtual string Name
		{
			get { return m_name; }
		}
		
		public PointCloudSource(string file)
		{
			m_filePath = file;
			m_name = Path.GetFileName(FilePath);
		}

		public override string ToString()
		{
			return string.Format("{0}", Name);
		}
	}
}
