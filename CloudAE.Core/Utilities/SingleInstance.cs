using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Ipc;
using System.Runtime.Serialization.Formatters;
using System.Threading;
using System.Windows;
using System.Windows.Threading;

namespace CloudAE.Core.Util
{
	/// <summary>
	/// This class checks to make sure that only one instance of this application is running at a time.
	/// 
	/// Based on:
	/// http://blogs.microsoft.co.il/blogs/arik/archive/2010/05/28/wpf-single-instance-application.aspx
	/// </summary>
	public static class SingleInstance
	{
		#region Private Fields

		private const string ChannelNameSuffix = "SingeInstanceIPCChannel";
		private const string RemoteServiceName = "SingleInstanceApplicationService";
		private const string IpcProtocol = "ipc://";

		private static Mutex c_singleInstanceMutex;
		private static IpcServerChannel c_channel;
		private static Action<string[]> c_signalExternalCommandLineArgs;

		#endregion

		#region Public Methods

		/// <summary>
		/// Checks if the instance of the application attempting to start is the first instance. 
		/// If not, activates the first instance.
		/// </summary>
		/// <returns>True if this is the first instance of the application.</returns>
		public static bool InitializeAsFirstInstance(Action<string[]> signalExternalCommandLineArgs)
		{
			var appName = Application.Current.GetType().Assembly.ManifestModule.ScopeName;
			var windowsIdentity = System.Security.Principal.WindowsIdentity.GetCurrent();
			var keyUserName = windowsIdentity != null ? windowsIdentity.User.ToString() : String.Empty;

			string applicationIdentifier = string.Format("{0}{1}", appName, keyUserName);
			string channelName = string.Format("{0}:{1}", applicationIdentifier, ChannelNameSuffix);

			bool firstInstance;
			c_singleInstanceMutex = new Mutex(true, applicationIdentifier, out firstInstance);
			if (firstInstance)
			{
				c_signalExternalCommandLineArgs = signalExternalCommandLineArgs;
				CreateRemoteService(channelName);
			}
			else
			{
				SignalFirstInstance(channelName, GetCommandLineArgs());
			}

			return firstInstance;
		}

		/// <summary>
		/// Cleans up single-instance code, clearing shared resources, mutexes, etc.
		/// </summary>
		public static void Cleanup()
		{
			if (c_singleInstanceMutex != null)
			{
				c_singleInstanceMutex.Close();
				c_singleInstanceMutex = null;
			}

			if (c_channel != null)
			{
				ChannelServices.UnregisterChannel(c_channel);
				c_channel = null;
			}
		}

		#endregion

		#region Private Methods

		/// <summary>
		/// Gets command line args.
		/// </summary>
		/// <returns>List of command line arg strings.</returns>
		private static string[] GetCommandLineArgs()
		{
			string[] args = null;
			if (AppDomain.CurrentDomain.ActivationContext == null)
				args = Environment.GetCommandLineArgs().Skip(1).ToArray();

			if (args == null)
				args = new string[] { };

			return args;
		}

		/// <summary>
		/// Creates a remote service for communication.
		/// </summary>
		/// <param name="channelName">Application's IPC channel name.</param>
		private static void CreateRemoteService(string channelName)
		{
			var serverProvider = new BinaryServerFormatterSinkProvider();
			serverProvider.TypeFilterLevel = TypeFilterLevel.Full;
			var props = new Dictionary<string, string>();

			props["name"] = channelName;
			props["portName"] = channelName;
			props["exclusiveAddressUse"] = "true";

			c_channel = new IpcServerChannel(props, serverProvider);

			ChannelServices.RegisterChannel(c_channel, true);

			var remoteService = new IPCRemoteService();
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
		private static void SignalFirstInstance(string channelName, string[] args)
		{
			var secondInstanceChannel = new IpcClientChannel();
			ChannelServices.RegisterChannel(secondInstanceChannel, true);

			string remotingServiceUrl = IpcProtocol + channelName + "/" + RemoteServiceName;

			// Obtain a reference to the remoting service exposed by the server i.e the first instance of the application
			var firstInstanceRemoteServiceReference = (IPCRemoteService)RemotingServices.Connect(typeof(IPCRemoteService), remotingServiceUrl);

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
			var args = arg as string[];
			if (args != null)
				ActivateFirstInstance(args.ToArray());

			return null;
		}

		/// <summary>
		/// Activates the first instance of the application with arguments from a second instance.
		/// </summary>
		/// <param name="args">List of arguments to supply the first instance of the application.</param>
		private static void ActivateFirstInstance(string[] args)
		{
			if (Application.Current == null)
				return;

			c_signalExternalCommandLineArgs(args);
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
					Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new DispatcherOperationCallback(ActivateFirstInstanceCallback), args);
				}
			}

			/// <summary>
			/// Remoting Object's lease expires after every 5 minutes by default. We need to 
			/// override the InitializeLifetimeService to ensure that lease never expires.
			/// </summary>
			/// <returns>Always null.</returns>
			public override object InitializeLifetimeService()
			{
				return null;
			}
		}

		#endregion
	}
}
