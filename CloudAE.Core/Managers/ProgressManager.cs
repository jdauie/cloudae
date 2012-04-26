using System;
using System.Diagnostics;

namespace CloudAE.Core
{
	public abstract class ProgressManager
	{
		private Action<string> m_logAction;
		private object m_userState;

		private ProgressManagerProcess m_currentProcess;

		public object UserState
		{
			get { return m_userState; }
		}

		public ProgressManager(object userState, Action<string> logAction)
		{
			m_userState = userState;
			m_logAction = logAction;
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

		public bool Update(float progressRatio)
		{
			return Update(progressRatio, null);
		}

		public bool Update(IProgress progress)
		{
			return Update(progress.Progress, null);
		}

		public bool Update(IProgress progress, object userState)
		{
			return Update(progress.Progress, userState);
		}

		public abstract bool Update(float progressRatio, object userState);

		public abstract bool IsCanceled();

		public ProgressManagerProcess StartProcess(string name)
		{
			ProgressManagerProcess process = null;

			if (m_currentProcess != null)
				process = m_currentProcess.StartProcess(name);
			else
				process = new ProgressManagerProcess(this, null, name);

			return process;
		}

		public void EndProcess(ProgressManagerProcess process)
		{
			m_currentProcess = process.Parent;
		}
	}
}
