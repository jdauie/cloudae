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
	public partial class App : Application
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

			ShowMainWindow();

			HandleArgs(e.Args);
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
			SingleInstance.Cleanup();
			base.OnExit(e);
			Context.Shutdown();
		}

		private void HandleArgs(string[] args)
		{
			string[] paths = args.Where(File.Exists).ToArray();
			if (paths.Length > 0)
				Context.AddToProcessingQueue(paths);
		}

		private void SignalExternalCommandLineArgs(string[] args)
		{
			Application.Current.Dispatcher.BeginInvoke(new Action(() =>
			{
				try
				{
					MainWindow.Activate();
				} catch { }
			}
			));

			HandleArgs(args);
		}
	}
}
