using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Diagnostics;

namespace CloudAE.Core
{
	public class ProgressManager
	{
		private BackgroundWorker m_worker;
		private DoWorkEventArgs m_args;
		private Action<string> m_logAction;

		public ProgressManager(BackgroundWorker worker, DoWorkEventArgs args, Action<string> logAction)
		{
			m_worker = worker;
			m_args = args;
			m_logAction = logAction;
		}

		public bool Update(float progressRatio)
		{
			return Update(progressRatio, null);
		}

		public bool Update(float progressRatio, object userState)
		{
			if ((m_worker.CancellationPending == true))
			{
				m_args.Cancel = true;
				return false;
			}

			m_worker.ReportProgress((int)(100 * progressRatio), userState);
			return true;
		}

		public void Log(string value, params object[] args)
		{
			string valueFormat = String.Format(value, args);
			m_logAction(valueFormat);
		}

		public void Log(Stopwatch stopwatch, string eventName, params object[] args)
		{
			stopwatch.Stop();
			string eventNameFormat = String.Format(eventName, args);
			Log("{1} in {0:0,.}s", stopwatch.ElapsedMilliseconds, eventNameFormat);
			stopwatch.Restart();
		}
	}
}
