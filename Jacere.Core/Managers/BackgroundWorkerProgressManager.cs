using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Diagnostics;

namespace Jacere.Core
{
	public class BackgroundWorkerProgressManager : ProgressManager
	{
		private ManagedBackgroundWorker m_worker;
		private DoWorkEventArgs m_args;

		public BackgroundWorkerProgressManager(ManagedBackgroundWorker worker, DoWorkEventArgs args, Action<string> logAction, Action<string> processAction)
			: this(worker, args, null, logAction, processAction)
		{
		}

		public BackgroundWorkerProgressManager(ManagedBackgroundWorker worker, DoWorkEventArgs args, object userState, Action<string> logAction, Action<string> processAction)
			: base(userState, logAction, processAction)
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
