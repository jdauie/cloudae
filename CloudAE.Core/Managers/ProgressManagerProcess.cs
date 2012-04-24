using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Diagnostics;

namespace CloudAE.Core
{
	public class ProgressManagerProcess : IDisposable
	{
		private readonly ProgressManager m_progressManager;
		private readonly Stopwatch m_stopwatch;
		private readonly Identity m_id;

		public ProgressManagerProcess(ProgressManager progressManager, string name)
		{
			m_progressManager = progressManager;
			m_id = IdentityManager.AcquireIdentity(name, IdentityType.Process);
			m_stopwatch = new Stopwatch();

			m_progressManager.Update(0.0f);
			m_stopwatch.Start();
		}

		public void Dispose()
		{
			m_stopwatch.Stop();
			m_progressManager.Update(100.0f);

			BufferManager.ReleaseBuffers(m_id);
		}

		public void Log(string value, params object[] args)
		{
			m_progressManager.Log(value, args);
		}

		public void LogTime(string value, params object[] args)
		{
			m_progressManager.Log(m_stopwatch, value, args);
		}

		public bool IsCanceled()
		{
			return m_progressManager.IsCanceled();
		}

		public bool Update(float progressRatio)
		{
			return m_progressManager.Update(progressRatio);
		}

		public bool Update(IProgress progress)
		{
			return m_progressManager.Update(progress.Progress);
		}

		public bool Update(float progressRatio, object userState)
		{
			return m_progressManager.Update(progressRatio, userState);
		}

		public BufferInstance AcquireBuffer()
		{
			return AcquireBuffer(false);
		}

		public BufferInstance AcquireBuffer(bool pin)
		{
			return BufferManager.AcquireBuffer(m_id, pin);
		}
	}
}
