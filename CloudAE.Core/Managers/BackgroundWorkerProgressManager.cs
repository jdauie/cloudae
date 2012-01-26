using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Diagnostics;

namespace CloudAE.Core
{
	public class BackgroundWorkerProgressManager : ProgressManager
	{
		private BackgroundWorker m_worker;
		private DoWorkEventArgs m_args;

		public BackgroundWorkerProgressManager(BackgroundWorker worker, DoWorkEventArgs args, Action<string> logAction)
			: base(logAction)
		{
			m_worker = worker;
			m_args = args;
		}

		public override bool Update(float progressRatio, object userState)
		{
			if ((m_worker.CancellationPending == true))
			{
				m_args.Cancel = true;
				return false;
			}

			m_worker.ReportProgress((int)(100 * progressRatio), userState);
			return true;
		}
	}
}
