using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace CloudAE.Core
{
	public abstract class PointCloudSource
	{
		public readonly string FilePath;
		
		private readonly string m_name;

		public virtual string Name
		{
			get { return m_name; }
		}
		
		public PointCloudSource(string file)
		{
			FilePath = file;
			m_name = Path.GetFileName(FilePath);
		}

		public override string ToString()
		{
			return string.Format("{0}", Name);
		}
	}
}
