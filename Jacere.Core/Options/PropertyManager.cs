using System;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Jacere.Core
{
	public static class PropertyManager
	{
		public static readonly string COMPANY_NAME;
		public static readonly string PRODUCT_NAME;

		public static readonly string APP_DATA_KEY;
		public static readonly string APP_DATA_DIR;
		public static readonly string APP_TEMP_DIR;

		private static readonly IPropertyManager c_manager;

		static PropertyManager()
		{
			var entryAssembly = Assembly.GetEntryAssembly();
			COMPANY_NAME = ((AssemblyCompanyAttribute[])entryAssembly.GetCustomAttributes(typeof(AssemblyCompanyAttribute), false)).Single().Company;
			PRODUCT_NAME = ((AssemblyProductAttribute[])entryAssembly.GetCustomAttributes(typeof(AssemblyProductAttribute), false)).Single().Product;

			string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
			APP_DATA_KEY = @"Software\" + COMPANY_NAME + @"\" + PRODUCT_NAME;
			APP_DATA_DIR = Path.Combine(localAppData, COMPANY_NAME, PRODUCT_NAME);
			APP_TEMP_DIR = Path.Combine(Path.GetTempPath(), COMPANY_NAME);

			c_manager = new RegistryPropertyManager();
		}

		public static PropertyName CreatePropertyName(string name)
		{
			return c_manager.CreatePropertyName(name);
		}

		private static PropertyName CreatePropertyName(string prefix, string name)
		{
			return c_manager.CreatePropertyName(prefix, name);
		}

		public static IPropertyState<T> Create<T>(PropertyName propertyName, T defaultValue)
		{
			return c_manager.Create(propertyName, defaultValue);
		}

		#region ISerializeStateBinary Handlers

		private static bool SetProperty(PropertyName name, ISerializeStateBinary value)
		{
			if (value == null)
				throw new ArgumentNullException("value");

			return c_manager.SetProperty(name, value);
		}

		private static bool GetProperty(PropertyName name, ISerializeStateBinary value)
		{
			if (value == null)
				throw new ArgumentNullException("value");

			return c_manager.GetProperty(name, value);
		}

		public static bool SetProperty(ISerializeStateBinary state, string prefix)
		{
			var name = CreatePropertyName(prefix, state.GetIdentifier());
			return SetProperty(name, state);
		}

		public static bool GetProperty(ISerializeStateBinary state, string prefix)
		{
			var name = CreatePropertyName(prefix, state.GetIdentifier());
			return GetProperty(name, state);
		}

		#endregion

		#region IPropertyState Handlers

		public static bool SetProperty(IPropertyState state)
		{
			if (state == null)
				throw new ArgumentNullException("state");
			
			return c_manager.SetProperty(state);
		}

		public static bool GetProperty(IPropertyState state)
		{
			if (state == null)
				throw new ArgumentNullException("state");

			return c_manager.GetProperty(state);
		}

		#endregion
	}
}
