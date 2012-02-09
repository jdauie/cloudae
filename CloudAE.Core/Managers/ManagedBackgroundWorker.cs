using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;

namespace CloudAE.Core
{
	public class ManagedBackgroundWorker : BackgroundWorker
	{
		private ProgressManager m_manager;

		public ProgressManager Manager
		{
			get { return m_manager; }
			set { m_manager = value; }
		}
	}
}
