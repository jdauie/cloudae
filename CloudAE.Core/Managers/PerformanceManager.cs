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
		public const string COUNTER_IO_SEEK = "SeekTime";
		public const string COUNTER_IO_READ = "ReadTime";

		private static Dictionary<string, long> c_counters;

		static PerformanceManager()
		{
			c_counters = new Dictionary<string, long>();
		}

		public static void UpdateCounter(string name, Stopwatch stopwatch)
		{
			UpdateCounter(name, stopwatch.ElapsedMilliseconds);
		}

		public static void UpdateCounter(string name, long timeInMs)
		{
			if (!c_counters.ContainsKey(name))
				c_counters.Add(name, timeInMs);
			else
				c_counters[name] += timeInMs;
		}

		public static void ResetCounter(string name)
		{
			c_counters[name] = 0;
		}

		public static string GetString()
		{
			return string.Join<string>(", ", c_counters.Select(kvp => String.Format("{0} = {1}", kvp.Key, kvp.Value)));
		}
	}
}
