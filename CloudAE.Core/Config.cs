using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CloudAE.Core
{
	public static class Config
	{
		private static bool m_contextStarted;

		private static bool m_showConsole;
		private static bool m_enableExtensionDiscovery;
		private static bool m_enableFactoryDiscovery;
		private static bool m_enablePropertyDiscovery;
		private static bool m_storePropertyRegistration;
		private static bool m_showAbstractTypesDuringDiscovery;

		private static Action<string, object[]> c_writeLineAction;

		public static bool ContextStarted
		{
			get { return m_contextStarted; }
			set
			{
				if (!value)
					throw new ArgumentException("ContextStarted can only be set to true.");

				if (m_contextStarted)
					throw new InvalidOperationException("ContextStarted has already been set.");

				m_contextStarted = true;
			}
		}

		public static bool ShowConsole
		{
			get { return m_showConsole; }
			set { m_showConsole = AttemptToAssignValue<bool>(value); }
		}

		public static bool EnableExtensionDiscovery
		{
			get { return m_enableExtensionDiscovery; }
			set { m_enableExtensionDiscovery = AttemptToAssignValue<bool>(value); }
		}

		public static bool EnableFactoryDiscovery
		{
			get { return m_enableFactoryDiscovery; }
			set { m_enableFactoryDiscovery = AttemptToAssignValue<bool>(value); }
		}

		public static bool EnablePropertyDiscovery
		{
			get { return m_enablePropertyDiscovery; }
			set { m_enablePropertyDiscovery = AttemptToAssignValue<bool>(value); }
		}

		public static bool StorePropertyRegistration
		{
			get { return m_storePropertyRegistration; }
			set { m_storePropertyRegistration = AttemptToAssignValue<bool>(value); }
		}

		public static bool ShowAbstractTypesDuringDiscovery
		{
			get { return m_showAbstractTypesDuringDiscovery; }
			set { m_showAbstractTypesDuringDiscovery = AttemptToAssignValue<bool>(value); }
		}

		static Config()
		{
			m_contextStarted = false;

			m_showConsole = true;
			m_enableExtensionDiscovery = true;
			m_enableFactoryDiscovery = true;
			m_enablePropertyDiscovery = true;
			m_storePropertyRegistration = true;
			m_showAbstractTypesDuringDiscovery = false;
		}

		private static T AttemptToAssignValue<T>(T value)
		{
			if (m_contextStarted)
				throw new InvalidOperationException("Context already initialized.");

			return value;
		}

		private static void WriteLine(string value, params object[] args)
		{
			c_writeLineAction(value, args);
		}

		public static void Write(Action<string, object[]> writeLine)
		{
			if (writeLine != null)
			{
				c_writeLineAction = writeLine;

				WriteLine("[Config]");
				WriteLine("  ShowConsole = {0}", ShowConsole);
				WriteLine("  EnableExtensionDiscovery = {0}", EnableExtensionDiscovery);
				WriteLine("  EnableFactoryDiscovery = {0}", EnableFactoryDiscovery);
				WriteLine("  EnablePropertyDiscovery = {0}", EnablePropertyDiscovery);
				WriteLine("  StorePropertyRegistration = {0}", StorePropertyRegistration);
				WriteLine("  ShowAbstractTypesDuringDiscovery = {0}", ShowAbstractTypesDuringDiscovery);
			}
		}
	}
}
