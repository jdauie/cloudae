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

		public string Name
		{
			get { return m_name; }
		}
		
		public PointCloudSource(string file)
		{
			FilePath = file;
			m_name = Path.GetFileNameWithoutExtension(FilePath);
		}

		public override string ToString()
		{
			return string.Format("{0}", Name);
		}
	}
}
