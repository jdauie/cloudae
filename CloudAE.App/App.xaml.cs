using System;
//using System.Runtime.InteropServices;
using System.Windows;

using CloudAE.Core;

namespace CloudAE.App
{
	/// <summary>
	/// Interaction logic for App.xaml
	/// </summary>
	public partial class App : Application
	{
		//[DllImport("kernel32.dll", SetLastError = true)]
		//static extern bool AllocConsole();

		//[DllImport("kernel32.dll", SetLastError = true)]
		//static extern bool FreeConsole();

		protected override void OnStartup(StartupEventArgs e)
		{
			//AllocConsole();

			SplashScreen appSplash = new SplashScreen("splash.png");
			appSplash.Show(false);
			{
				base.OnStartup(e);
				Context.Startup();
			}
			appSplash.Close(TimeSpan.FromMilliseconds(300));

			//FreeConsole();

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
