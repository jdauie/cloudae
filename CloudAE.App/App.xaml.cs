using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;

using CloudAE.Core;
using Jacere.Core.Util;

namespace CloudAE.App
{
	/// <summary>
	/// Interaction logic for App.xaml
	/// </summary>
	public partial class App
	{
		protected override void OnStartup(StartupEventArgs e)
		{
			if (!SingleInstance.InitializeAsFirstInstance(SignalExternalCommandLineArgs))
				Environment.Exit(0);

			var appSplash = new SplashScreen("splash.png");
			appSplash.Show(false);
			{
				base.OnStartup(e);
			    Config.ShowConsole = true;
				Context.Startup();
			}
			appSplash.Close(TimeSpan.FromMilliseconds(300));

			if (true)
			{
				ShutdownMode = ShutdownMode.OnExplicitShutdown;
				Context.ProcessingQueueEmpty += Shutdown;

				Context.Log += OnLog;
				Context.ProcessingProcessChanged += OnProcessingProcessChanged;
				Context.ProcessingProgressChanged += OnProcessingProgressChanged;
			}
			else
			{
				ShowMainWindow();
			}

			HandleArgs(e.Args);
		}

		private void OnLog(string value)
		{
			Console.WriteLine(value);
		}

		private static void OnProcessingProcessChanged(string process)
		{
			//Console.WriteLine();
		}

		private static void OnProcessingProgressChanged(int progressPercentage)
		{
			//if (progressPercentage == 0)
				return;

			Console.Write("\b\b\b");

			if (progressPercentage != 100)
				Console.Write("{0:000}", progressPercentage);
		}

		private void ShowMainWindow()
		{
            var window = new MainWindow();
            //var window = new MapWindow();
            ShutdownMode = System.Windows.ShutdownMode.OnMainWindowClose;
			MainWindow = window;
			window.Show();
		}

		protected override void OnExit(ExitEventArgs e)
		{
			if (true)
			{
				Console.ReadKey();
			}

			SingleInstance.Cleanup();
			base.OnExit(e);
			Context.Shutdown();
		}

		private static void HandleArgs(string[] args)
		{
			var paths = args.Where(File.Exists).ToArray();
			if (paths.Length > 0)
				Context.AddToProcessingQueue(paths);
		}

		private void SignalExternalCommandLineArgs(string[] args)
		{
			if (MainWindow != null)
			{
				Current.Dispatcher.BeginInvoke(new Action(() =>
				{
					try
					{
						MainWindow.Activate();
					}
					catch { }
				}
				));
			}

			HandleArgs(args);
		}
	}
}
