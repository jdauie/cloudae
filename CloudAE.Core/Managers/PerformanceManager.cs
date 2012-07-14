using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Diagnostics;

namespace CloudAE.Core
{
	public static class PerformanceManager
	{
		public const string COUNTER_READ_TIME = "ReadTime";
		public const string COUNTER_READ_BYTES = "ReadBytes";

		public const string COUNTER_WRITE_TIME = "WriteTime";
		public const string COUNTER_WRITE_BYTES = "WriteBytes";

		private static List<PerformanceManagementInstance> c_instances;
		private static PerformanceManagementInstance c_current;

		static PerformanceManager()
		{
			c_instances = new List<PerformanceManagementInstance>();
			c_current = null;
		}

		//private static void CheckState()
		//{
		//    if (c_current == null)
		//        throw new InvalidOperationException("There is no active performance management instance.");
		//}

		public static void Start(string name)
		{
			if (c_current != null)
			{
				c_instances.Add(c_current);
				c_current = null;
			}

			c_current = new PerformanceManagementInstance(name);
		}

		public static void UpdateReadBytes(long length, Stopwatch stopwatch)
		{
			if (c_current != null)
			{
				c_current.AppendValue(COUNTER_READ_BYTES, length);
				c_current.AppendValue(COUNTER_READ_TIME, stopwatch.ElapsedTicks);
			}
		}

		public static void UpdateWriteBytes(long length, Stopwatch stopwatch)
		{
			if (c_current != null)
			{
				c_current.AppendValue(COUNTER_WRITE_BYTES, length);
				c_current.AppendValue(COUNTER_WRITE_TIME, stopwatch.ElapsedTicks);
			}
		}

		public static TransferRate GetReadSpeed()
		{
			if (c_current != null)
			{
				long readBytes = c_current.GetValue(COUNTER_READ_BYTES);
				long readTime = c_current.GetValue(COUNTER_READ_TIME);
				return new TransferRate(readBytes, readTime);
			}
			return TransferRate.Empty;
		}

		public static TransferRate GetWriteSpeed()
		{
			if (c_current != null)
			{
				long writeBytes = c_current.GetValue(COUNTER_WRITE_BYTES);
				long writeTime = c_current.GetValue(COUNTER_WRITE_TIME);
				return new TransferRate(writeBytes, writeTime);
			}
			return TransferRate.Empty;
		}

		//public static string GetString()
		//{
		//    return string.Join<string>(", ", c_counters.Select(kvp => String.Format("{0} = {1}", kvp.Key, kvp.Value)));
		//}
	}

	class PerformanceManagementInstance
	{
		private readonly string m_name;
		private readonly Dictionary<string, long> m_counters;

		public PerformanceManagementInstance(string name)
		{
			m_name = name;
			m_counters = new Dictionary<string, long>();
		}

		public void AppendValue(string name, long value)
		{
			if (!m_counters.ContainsKey(name))
				m_counters.Add(name, value);
			else
				m_counters[name] += value;
		}

		public long GetValue(string name)
		{
			long value = 0;
			m_counters.TryGetValue(name, out value);
			return value;
		}
	}

	public struct TransferRate
	{
		public static readonly TransferRate Empty;

		private readonly long m_bytes;
		private readonly long m_time;

		static TransferRate()
		{
			Empty = new TransferRate(0, 0);
		}

		public TransferRate(long bytes, long time)
		{
			m_bytes = bytes;
			m_time = time;
		}

		public override string ToString()
		{
			double seconds = (double)m_time / Stopwatch.Frequency;
			long bytesPerSecond = (long)(m_bytes / seconds);
			return string.Format("{0} in {1:f}s @ {2}ps", m_bytes.ToSize(), seconds, bytesPerSecond.ToSize());
		}
	}
}
