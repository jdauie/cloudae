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
		private ManagedBackgroundWorker m_worker;
		private DoWorkEventArgs m_args;

		public BackgroundWorkerProgressManager(ManagedBackgroundWorker worker, DoWorkEventArgs args, Action<string> logAction)
			: this(worker, args, null, logAction)
		{
		}

		public BackgroundWorkerProgressManager(ManagedBackgroundWorker worker, DoWorkEventArgs args, object userState, Action<string> logAction)
			: base(userState, logAction)
		{
			m_worker = worker;
			m_args = args;

			m_worker.Manager = this;
		}

		public override bool Update(float progressRatio, object userState)
		{
			if (IsCanceled())
				return false;

			m_worker.ReportProgress((int)(100 * progressRatio), userState);
			return true;
		}

		public override bool IsCanceled()
		{
			if ((m_worker.CancellationPending))
			{
				m_args.Cancel = true;
				return true;
			}

			return false;
		}
	}
}
