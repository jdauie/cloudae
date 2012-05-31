using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;

namespace CloudAE.Core
{
	public class ProgressManagerProcess : IDisposable
	{
		private readonly ProgressManager m_progressManager;
		private readonly Stopwatch m_stopwatch;
		private readonly Identity m_id;

		private readonly ProgressManagerProcess m_parent;
		private readonly List<ProgressManagerProcess> m_children;

		public ProgressManagerProcess Parent
		{
			get { return m_parent; }
		}

		public ProgressManagerProcess(ProgressManager progressManager, ProgressManagerProcess parent, string name)
		{
			m_progressManager = progressManager;
			m_id = IdentityManager.AcquireIdentity(name, IdentityType.Process);
			m_stopwatch = new Stopwatch();

			m_parent = parent;
			m_children = new List<ProgressManagerProcess>();

			m_progressManager.Update(0.0f);
			m_stopwatch.Start();

			int i = 0;
			ProgressManagerProcess proc = this;
			while ((proc = proc.Parent) != null)
				i++;

			//Context.WriteLine(string.Format("{0}{1} -> Start", string.Empty.PadRight(2 * i), m_id));
			//Context.WriteLine("{0}{1} {2}", string.Empty.PadRight(2 * i), m_id, "{");
			Context.WriteLine("{0}{1}", string.Empty.PadRight(2 * i), m_id);
		}

		public void Dispose()
		{
			m_stopwatch.Stop();
			m_progressManager.Update(100.0f);

			BufferManager.ReleaseBuffers(m_id);

			//int i = 0;
			//ProgressManagerProcess proc = this;
			//while ((proc = proc.Parent) != null)
			//    i++;

			m_progressManager.EndProcess(this);

			//Context.WriteLine(string.Format("{0}{1} -> End", string.Empty.PadRight(2 * i), m_id));
			//Context.WriteLine("{0}{1}", string.Empty.PadRight(2 * i), "}");
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

		/// <summary>
		/// Can these buffers be left available for use by scoped child processes?
		/// Obviously, I don't have a mechanism for that now.
		/// </summary>
		/// <returns></returns>
		public BufferInstance AcquireBuffer()
		{
			return AcquireBuffer(false);
		}

		public BufferInstance AcquireBuffer(bool pin)
		{
			return BufferManager.AcquireBuffer(m_id, pin);
		}

		public ProgressManagerProcess StartProcess(string name)
		{
			var process = new ProgressManagerProcess(m_progressManager, this, name);
			return process;
		}

		public void Add(ProgressManagerProcess process)
		{
			m_children.Add(process);
		}
	}
}
