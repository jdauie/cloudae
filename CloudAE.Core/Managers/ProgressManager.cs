using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Diagnostics;

namespace CloudAE.Core
{
	public abstract class ProgressManager
	{
		private Action<string> m_logAction;
		private object m_userState;

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

		public abstract bool Update(float progressRatio, object userState);
	}
}
