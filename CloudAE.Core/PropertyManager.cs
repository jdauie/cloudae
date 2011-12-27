using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Microsoft.Win32;
using System.Reflection;

namespace CloudAE.Core
{
	public static class PropertyManager
	{
		public static readonly string COMPANY_NAME;
		public static readonly string PRODUCT_NAME;

		public static readonly string APP_DATA_KEY;
		public static readonly string APP_DATA_DIR;
		public static readonly string APP_TEMP_DIR;

		static PropertyManager()
		{
			Assembly entryAssembly = Assembly.GetEntryAssembly();
			COMPANY_NAME = ((AssemblyCompanyAttribute[])entryAssembly.GetCustomAttributes(typeof(AssemblyCompanyAttribute), false)).Single().Company;
			PRODUCT_NAME = ((AssemblyProductAttribute[])entryAssembly.GetCustomAttributes(typeof(AssemblyProductAttribute), false)).Single().Product;

			string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
			APP_DATA_KEY = @"Software\" + COMPANY_NAME + @"\" + PRODUCT_NAME;
			APP_DATA_DIR = Path.Combine(localAppData, COMPANY_NAME, PRODUCT_NAME);
			APP_TEMP_DIR = Path.Combine(Path.GetTempPath(), COMPANY_NAME);
		}

		public static PropertyName ParsePropertyName(string name)
		{
			string[] names = name.Split(new char[] { '\\' }, StringSplitOptions.RemoveEmptyEntries);
			string valueName = names[names.Length - 1];

			string subKeyPath = APP_DATA_KEY;
			for (int i = 0; i < names.Length - 1; i++)
				subKeyPath += @"\" + names[i];

			return new PropertyName(subKeyPath, valueName);
		}

		public static bool SetProperty(string name, ISerializeStateBinary value)
		{
			if (value == null)
				throw new ArgumentNullException("value");

			PropertyName property = ParsePropertyName(name);
			bool success = false;
			
			try
			{
				using (RegistryKey hiveKey = Registry.CurrentUser)
				{
					using (RegistryKey subKey = hiveKey.CreateSubKey(property.Path))
					{
						if (subKey != null)
						{
							MemoryStream ms = new MemoryStream();
							using (BinaryWriter writer = new BinaryWriter(ms))
							{
								writer.Write(value);
								writer.Flush();
								subKey.SetValue(property.Name, ms.ToArray(), RegistryValueKind.Binary);
							}
							success = true;
						}
					}
				}
			}
			catch { }

			return success;
		}

		public static bool GetProperty(string name, ISerializeStateBinary value)
		{
			if (value == null)
				throw new ArgumentNullException("value");

			PropertyName property = ParsePropertyName(name);
			bool success = false;

			try
			{
				using (RegistryKey hiveKey = Registry.CurrentUser)
				{
					using (RegistryKey subKey = hiveKey.OpenSubKey(property.Path))
					{
						if (subKey != null)
						{
							byte[] bytes = subKey.GetValue(property.Name) as byte[];
							if (bytes != null)
							{
								MemoryStream ms = new MemoryStream(bytes);
								using (BinaryReader reader = new BinaryReader(ms))
									value.Deserialize(reader);
								success = true;
							}
						}
					}
				}
			}
			catch { }

			return success;
		}
	}

	public class PropertyName
	{
		public readonly string Path;
		public readonly string Name;

		public PropertyName(string path, string name)
		{
			Path = path;
			Name = name;
		}
	}
}
