using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;

using CloudAE.Core;
using CloudAE.Core.Util;

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

			SplashScreen appSplash = new SplashScreen("splash.png");
			appSplash.Show(false);
			{
				base.OnStartup(e);
				Context.Startup();
			}
			appSplash.Close(TimeSpan.FromMilliseconds(300));

			ShowMainWindow();

			HandleArgs(e.Args);
		}

		private void ShowMainWindow()
		{
			MainWindow window = new MainWindow();
			ShutdownMode = System.Windows.ShutdownMode.OnMainWindowClose;
			MainWindow = window;
			window.Show();
		}

		protected override void OnExit(ExitEventArgs e)
		{
			base.OnExit(e);

			SingleInstance.Cleanup();

			Context.Shutdown();
		}

		private void HandleArgs(string[] args)
		{
			string[] paths = args.Where(a => File.Exists(a)).ToArray();
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
