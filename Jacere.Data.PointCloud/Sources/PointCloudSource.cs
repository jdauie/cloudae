using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;

namespace CloudAE.Core
{
	public abstract class PointCloudSource
	{
		private readonly Identity m_id;

		private readonly FileHandlerBase m_handler;
		
		private readonly string m_name;

		public FileHandlerBase FileHandler
		{
			get { return m_handler; }
		}

		public virtual string FilePath
		{
			get { return m_handler.FilePath; }
		}

		public virtual string Name
		{
			get { return m_name; }
		}

		protected Identity ID
		{
			get { return m_id; }
		}

		protected PointCloudSource(FileHandlerBase handler)
		{
			m_id = IdentityManager.AcquireIdentity(GetType().Name);

			m_handler = handler;
			m_name = Path.GetFileName(FilePath);
		}

		public override string ToString()
		{
			return string.Format("{0}", Name);
		}
	}
}
