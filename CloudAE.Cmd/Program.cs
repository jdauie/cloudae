using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using CloudAE.Core;
using Jacere.Core.Util;

namespace CloudAE.Cmd
{
	class Program
	{
		private static AutoResetEvent c_event;

		static void Main(string[] args)
		{
			if (!SingleInstance.InitializeAsFirstInstance(SignalExternalCommandLineArgs))
				Environment.Exit(0);

			Context.Startup();

			Context.ProcessingQueueEmpty += Shutdown;

			Context.Log += OnLog;
			Context.ProcessingProcessChanged += OnProcessingProcessChanged;
			Context.ProcessingProgressChanged += OnProcessingProgressChanged;

			HandleArgs(args);

			c_event = new AutoResetEvent(false);
			c_event.WaitOne();
		}

		private static void Shutdown()
		{
			SingleInstance.Cleanup();
			Context.Shutdown();
			c_event.Set();
		}

		private static void OnLog(string value)
		{
			Console.WriteLine(value);
		}

		private static void OnProcessingProcessChanged(string process)
		{
			Console.WriteLine(process);
		}

		private static void OnProcessingProgressChanged(int progressPercentage)
		{
			Console.Write("{0}%\r".PadRight(4), progressPercentage);
		}

		private static void HandleArgs(string[] args)
		{
			var paths = args.Where(File.Exists).ToArray();
			if (paths.Length > 0)
				Context.AddToProcessingQueue(paths);
		}

		private static void SignalExternalCommandLineArgs(string[] args)
		{
			HandleArgs(args);
		}
	}
}
