using System;
using System.IO;
using System.Linq;
using System.Windows;

using CloudAE.Core;
using System.Windows.Threading;
using System.Threading;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Ipc;
using System.Runtime.Remoting;
using System.Collections.Generic;
using System.Runtime.Serialization.Formatters;

namespace CloudAE.App
{
	/// <summary>
	/// Interaction logic for App.xaml
	/// </summary>
	public partial class App : Application, ISingleInstanceApp
	{
		protected override void OnStartup(StartupEventArgs e)
		{
			if (!SingleInstance<App>.InitializeAsFirstInstance("asdfzxdfc98gvz89 v87zjzjd8fgv9zsd"))
			{
				Environment.Exit(0);
				//Shutdown();
				//return;
			}


			//WpfSingleInstance.Make();

			SplashScreen appSplash = new SplashScreen("splash.png");
			appSplash.Show(false);
			{
				base.OnStartup(e);
				Context.Startup();
			}
			appSplash.Close(TimeSpan.FromMilliseconds(300));

			// handle any args
			// (e.g. inputs, silent, scripts?)

			// -i asdf.las
			
			if (true)
			{
				ShowMainWindow();

				if (e.Args.Length >= 1)
				{
					string[] paths = e.Args.Where(a => File.Exists(a)).ToArray();
					if(paths.Length > 0)
						Context.AddToProcessingQueue(paths);
				}
			}
			else
			{
				ShutdownMode = System.Windows.ShutdownMode.OnExplicitShutdown;
				// do stuff
				Shutdown();
			}
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

			SingleInstance<App>.Cleanup();

			Context.Shutdown();
		}

		#region ISingleInstanceApp Members

		public bool SignalExternalCommandLineArgs(IList<string> args)
		{
			Application.Current.Dispatcher.BeginInvoke(new Action(() =>
			{
				try
				{
					Application.Current.MainWindow.Activate();
				}
				catch { }
			}
			));

			// handle args

			string[] paths = args.Where(a => File.Exists(a)).ToArray();
			if (paths.Length > 0)
				Context.AddToProcessingQueue(paths);

			return true;
		}

		#endregion
	}




	public interface ISingleInstanceApp
	{
		bool SignalExternalCommandLineArgs(IList<string> args);
	}

	/// <summary>
	/// This class checks to make sure that only one instance of 
	/// this application is running at a time.
	/// </summary>
	/// <remarks>
	/// Note: this class should be used with some caution, because it does no
	/// security checking. For example, if one instance of an app that uses this class
	/// is running as Administrator, any other instance, even if it is not
	/// running as Administrator, can activate it with command line arguments.
	/// For most apps, this will not be much of an issue.
	/// </remarks>
	public static class SingleInstance<TApplication>
				where TApplication : Application, ISingleInstanceApp
	{
		#region Private Fields

		/// <summary>
		/// String delimiter used in channel names.
		/// </summary>
		private const string Delimiter = ":";

		/// <summary>
		/// Suffix to the channel name.
		/// </summary>
		private const string ChannelNameSuffix = "SingeInstanceIPCChannel";

		/// <summary>
		/// Remote service name.
		/// </summary>
		private const string RemoteServiceName = "SingleInstanceApplicationService";

		/// <summary>
		/// IPC protocol used (string).
		/// </summary>
		private const string IpcProtocol = "ipc://";

		/// <summary>
		/// Application mutex.
		/// </summary>
		private static Mutex singleInstanceMutex;

		/// <summary>
		/// IPC channel for communications.
		/// </summary>
		private static IpcServerChannel channel;

		/// <summary>
		/// List of command line arguments for the application.
		/// </summary>
		private static IList<string> commandLineArgs;

		#endregion

		#region Public Properties

		/// <summary>
		/// Gets list of command line arguments for the application.
		/// </summary>
		public static IList<string> CommandLineArgs
		{
			get { return commandLineArgs; }
		}

		#endregion

		#region Public Methods

		/// <summary>
		/// Checks if the instance of the application attempting to start is the first instance. 
		/// If not, activates the first instance.
		/// </summary>
		/// <returns>True if this is the first instance of the application.</returns>
		public static bool InitializeAsFirstInstance(string uniqueName)
		{
			commandLineArgs = GetCommandLineArgs(uniqueName);

			// Build unique application Id and the IPC channel name.
			string applicationIdentifier = uniqueName + Environment.UserName;

			string channelName = String.Concat(applicationIdentifier, Delimiter, ChannelNameSuffix);

			// Create mutex based on unique application Id to check if this is the first instance of the application. 
			bool firstInstance;
			singleInstanceMutex = new Mutex(true, applicationIdentifier, out firstInstance);
			if (firstInstance)
			{
				CreateRemoteService(channelName);
			}
			else
			{
				SignalFirstInstance(channelName, commandLineArgs);
			}

			return firstInstance;
		}

		/// <summary>
		/// Cleans up single-instance code, clearing shared resources, mutexes, etc.
		/// </summary>
		public static void Cleanup()
		{
			if (singleInstanceMutex != null)
			{
				singleInstanceMutex.Close();
				singleInstanceMutex = null;
			}

			if (channel != null)
			{
				ChannelServices.UnregisterChannel(channel);
				channel = null;
			}
		}

		#endregion

		#region Private Methods

		/// <summary>
		/// Gets command line args - for ClickOnce deployed applications, command line args may not be passed directly, they have to be retrieved.
		/// </summary>
		/// <returns>List of command line arg strings.</returns>
		private static IList<string> GetCommandLineArgs(string uniqueApplicationName)
		{
			string[] args = null;
			if (AppDomain.CurrentDomain.ActivationContext == null)
				args = Environment.GetCommandLineArgs();

			if (args == null)
				args = new string[] { };

			return new List<string>(args);
		}

		/// <summary>
		/// Creates a remote service for communication.
		/// </summary>
		/// <param name="channelName">Application's IPC channel name.</param>
		private static void CreateRemoteService(string channelName)
		{
			BinaryServerFormatterSinkProvider serverProvider = new BinaryServerFormatterSinkProvider();
			serverProvider.TypeFilterLevel = TypeFilterLevel.Full;
			var props = new Dictionary<string, string>();

			props["name"] = channelName;
			props["portName"] = channelName;
			props["exclusiveAddressUse"] = "false";

			// Create the IPC Server channel with the channel properties
			channel = new IpcServerChannel(props, serverProvider);

			// Register the channel with the channel services
			ChannelServices.RegisterChannel(channel, true);

			// Expose the remote service with the REMOTE_SERVICE_NAME
			IPCRemoteService remoteService = new IPCRemoteService();
			RemotingServices.Marshal(remoteService, RemoteServiceName);
		}

		/// <summary>
		/// Creates a client channel and obtains a reference to the remoting service exposed by the server - 
		/// in this case, the remoting service exposed by the first instance. Calls a function of the remoting service 
		/// class to pass on command line arguments from the second instance to the first and cause it to activate itself.
		/// </summary>
		/// <param name="channelName">Application's IPC channel name.</param>
		/// <param name="args">
		/// Command line arguments for the second instance, passed to the first instance to take appropriate action.
		/// </param>
		private static void SignalFirstInstance(string channelName, IList<string> args)
		{
			IpcClientChannel secondInstanceChannel = new IpcClientChannel();
			ChannelServices.RegisterChannel(secondInstanceChannel, true);

			string remotingServiceUrl = IpcProtocol + channelName + "/" + RemoteServiceName;

			// Obtain a reference to the remoting service exposed by the server i.e the first instance of the application
			IPCRemoteService firstInstanceRemoteServiceReference = (IPCRemoteService)RemotingServices.Connect(typeof(IPCRemoteService), remotingServiceUrl);

			// Check that the remote service exists, in some cases the first instance may not yet have created one, in which case
			// the second instance should just exit
			if (firstInstanceRemoteServiceReference != null)
			{
				// Invoke a method of the remote service exposed by the first instance passing on the command line
				// arguments and causing the first instance to activate itself
				firstInstanceRemoteServiceReference.InvokeFirstInstance(args);
			}
		}

		/// <summary>
		/// Callback for activating first instance of the application.
		/// </summary>
		/// <param name="arg">Callback argument.</param>
		/// <returns>Always null.</returns>
		private static object ActivateFirstInstanceCallback(object arg)
		{
			// Get command line args to be passed to first instance
			IList<string> args = arg as IList<string>;
			ActivateFirstInstance(args);
			return null;
		}

		/// <summary>
		/// Activates the first instance of the application with arguments from a second instance.
		/// </summary>
		/// <param name="args">List of arguments to supply the first instance of the application.</param>
		private static void ActivateFirstInstance(IList<string> args)
		{
			// Set main window state and process command line args
			if (Application.Current == null)
			{
				return;
			}

			((TApplication)Application.Current).SignalExternalCommandLineArgs(args);
		}

		#endregion

		#region Private Classes

		/// <summary>
		/// Remoting service class which is exposed by the server i.e the first instance and called by the second instance
		/// to pass on the command line arguments to the first instance and cause it to activate itself.
		/// </summary>
		private class IPCRemoteService : MarshalByRefObject
		{
			/// <summary>
			/// Activates the first instance of the application.
			/// </summary>
			/// <param name="args">List of arguments to pass to the first instance.</param>
			public void InvokeFirstInstance(IList<string> args)
			{
				if (Application.Current != null)
				{
					// Do an asynchronous call to ActivateFirstInstance function
					Application.Current.Dispatcher.BeginInvoke(
						DispatcherPriority.Normal, new DispatcherOperationCallback(SingleInstance<TApplication>.ActivateFirstInstanceCallback), args);
				}
			}

			/// <summary>
			/// Remoting Object's ease expires after every 5 minutes by default. We need to override the InitializeLifetimeService class
			/// to ensure that lease never expires.
			/// </summary>
			/// <returns>Always null.</returns>
			public override object InitializeLifetimeService()
			{
				return null;
			}
		}

		#endregion
	}






	public static class WpfSingleInstance
	{
		/// <summary>
		/// Processing single instance in <see cref="SingleInstanceModes"/> <see cref="SingleInstanceModes.ForEveryUser"/> mode.
		/// </summary>
		internal static void Make()
		{
			Make(SingleInstanceModes.ForEveryUser);
		}

		/// <summary>
		/// Processing single instance.
		/// </summary>
		/// <param name="singleInstanceModes"></param>
		internal static void Make(SingleInstanceModes singleInstanceModes)
		{
			var appName = Application.Current.GetType().Assembly.ManifestModule.ScopeName;

			var windowsIdentity = System.Security.Principal.WindowsIdentity.GetCurrent();
			var keyUserName = windowsIdentity != null ? windowsIdentity.User.ToString() : String.Empty;

			// Be careful! Max 260 chars!
			var eventWaitHandleName = string.Format(
				"{0}{1}",
				appName,
				singleInstanceModes == SingleInstanceModes.ForEveryUser ? keyUserName : String.Empty
				);

			try
			{
				using (var eventWaitHandle = EventWaitHandle.OpenExisting(eventWaitHandleName))
				{
					// It informs first instance about other startup attempting.
					eventWaitHandle.Set();
				}

				// Let's terminate this posterior startup.
				// For that exit no interceptions.
				Environment.Exit(0);
			}
			catch
			{
				// It's first instance.

				// Register EventWaitHandle.
				using (var eventWaitHandle = new EventWaitHandle(false, EventResetMode.AutoReset, eventWaitHandleName))
				{
					// TODO unregister this when done
					RegisteredWaitHandle registeredWaitHandle = ThreadPool.RegisterWaitForSingleObject(eventWaitHandle, OtherInstanceAttemptedToStart, null, Timeout.Infinite, false);
				}

				RemoveApplicationsStartupDeadlockForStartupCrushedWindows();
			}
		}

		private static void OtherInstanceAttemptedToStart(Object state, Boolean timedOut)
		{
			RemoveApplicationsStartupDeadlockForStartupCrushedWindows();
			Application.Current.Dispatcher.BeginInvoke(new Action(() =>
				{
					try
					{
						Application.Current.MainWindow.Activate();
					} catch { }
				}
			));
		}

		internal static DispatcherTimer AutoExitAplicationIfStartupDeadlock;

		public static void RemoveApplicationsStartupDeadlockForStartupCrushedWindows()
		{
			Application.Current.Dispatcher.BeginInvoke(new Action(() =>
			{
				AutoExitAplicationIfStartupDeadlock =
					new DispatcherTimer(
						TimeSpan.FromSeconds(6),
						DispatcherPriority.ApplicationIdle,
						(o, args) =>
						{
							if (Application.Current.Windows.Cast<Window>().Count(window => !Double.IsNaN(window.Left)) == 0)
							{
								// For that exit no interceptions.
								Environment.Exit(0);
							}
						},
						Application.Current.Dispatcher
					);
			}),
				DispatcherPriority.ApplicationIdle
				);
		}
	}

	public enum SingleInstanceModes
	{
		/// <summary>
		/// Do nothing.
		/// </summary>
		NotInited = 0,

		/// <summary>
		/// Every user can have own single instance.
		/// </summary>
		ForEveryUser,
	}
}
