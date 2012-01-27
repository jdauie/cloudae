using System;
using System.Windows;

using CloudAE.Core;

namespace CloudAE.App
{
	/// <summary>
	/// Interaction logic for App.xaml
	/// </summary>
	public partial class App : Application
	{
		protected override void OnStartup(StartupEventArgs e)
		{
			SplashScreen appSplash = new SplashScreen("splash.png");
			appSplash.Show(false);
			{
				base.OnStartup(e);
				Context.Startup();
			}
			appSplash.Close(TimeSpan.FromMilliseconds(300));

			// handle any args
			// (e.g. inputs, silent, scripts?)

			ShowMainWindow();
		}

		private void ShowMainWindow()
		{
			MainWindow window = new MainWindow();
			window.Show();
		}

		protected override void OnExit(ExitEventArgs e)
		{
			base.OnExit(e);

			Context.Shutdown();
		}
	}
}
