using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using CloudAE.Core;
using Jacere.Core.Util;
using Mono.Options;

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

			bool show_help = false;
			bool clean_cache = false;

			var p = new OptionSet() {
				{"h|help", "show this message and exit.", v => show_help = (v != null)},
				{"clean", "clean cache.", v => clean_cache = true},
			};

			var extra = p.Parse(args);

			if (show_help)
			{
				ShowHelp(p);
				Shutdown();
			}
			else
			{
				if (clean_cache)
				{
					Cache.Clear();
				}

				HandleArgs(extra);

				c_event = new AutoResetEvent(false);
				c_event.WaitOne();
			}
		}

		static void ShowHelp(OptionSet p)
		{
			Console.WriteLine("Usage: app [OPTIONS] files");
			Console.WriteLine("description.");
			Console.WriteLine();
			Console.WriteLine("Options:");
			p.WriteOptionDescriptions(Console.Out);
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

		private static void HandleArgs(IEnumerable<string> args)
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
