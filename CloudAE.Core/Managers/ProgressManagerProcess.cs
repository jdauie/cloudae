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
		private readonly string m_name;
		private readonly Stopwatch m_stopwatch;

		public ProgressManagerProcess(ProgressManager progressManager, string name)
		{
			m_progressManager = progressManager;
			m_name = name;
			m_stopwatch = new Stopwatch();

			m_progressManager.Update(0.0f);
			m_stopwatch.Start();
		}

		public void Dispose()
		{
			m_stopwatch.Stop();
			m_progressManager.Update(100.0f);

			BufferManager.ReleaseBuffers(m_name);
		}

		public void Log(string value, params object[] args)
		{
			m_progressManager.Log(m_stopwatch, value, args);
		}

		public bool Update(float progressRatio)
		{
			return m_progressManager.Update(progressRatio);
		}

		public bool Update(float progressRatio, object userState)
		{
			return m_progressManager.Update(progressRatio, userState);
		}

		public byte[] AcquireBuffer()
		{
			return BufferManager.AcquireBuffer(m_name);
		}

		public void ReleaseBuffer(byte[] buffer)
		{
			BufferManager.ReleaseBuffer(buffer);
		}
	}
}
