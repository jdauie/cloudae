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

			string identifier = String.Join<string>(@"\", names);

			return new PropertyName(identifier, subKeyPath, valueName);
		}

		#region ISerializeStateBinary Handlers

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

		#endregion

		#region IPropertyState Handlers

		public static bool SetProperty(IPropertyState state)
		{
			if (state == null)
				throw new ArgumentNullException("state");

			bool success = false;

			try
			{
				using (RegistryKey hiveKey = Registry.CurrentUser)
				{
					using (RegistryKey subKey = hiveKey.CreateSubKey(state.Property.Path))
					{
						if (subKey != null)
						{
							subKey.SetValue(state.Property.Name, state.GetConvertedValue(), state.ValueKind);
							success = true;
						}
					}
				}
			}
			catch { }

			return success;
		}

		public static bool GetProperty(IPropertyState state)
		{
			if (state == null)
				throw new ArgumentNullException("state");

			bool success = false;

			try
			{
				using (RegistryKey hiveKey = Registry.CurrentUser)
				{
					using (RegistryKey subKey = hiveKey.OpenSubKey(state.Property.Path))
					{
						if (subKey != null)
						{
							object value = subKey.GetValue(state.Property.Name);
							if (value != null)
							{
								state.SetConvertedValue(value);
								success = true;
							}
						}
					}
				}
			}
			catch { }

			return success;
		}

		#endregion
	}

	public class PropertyName : IEquatable<PropertyName>
	{
		public readonly string ID;
		public readonly string Path;
		public readonly string Name;

		public PropertyName(string id, string path, string name)
		{
			ID  = id;
			Path = path;
			Name = name;
		}

		public override int GetHashCode()
		{
			return ID.GetHashCode();
		}

		public override bool Equals(object obj)
		{
			return Equals(obj as PropertyName);
		}

		public bool Equals(PropertyName other)
		{
			return (other != null && ID.Equals(other.ID, StringComparison.OrdinalIgnoreCase));
		}

		public override string ToString()
		{
			return ID;
		}
	}

	public interface IPropertyState
	{
		PropertyName Property { get; }
		RegistryValueKind ValueKind { get; }

		object GetConvertedValue();
		void SetConvertedValue(object value);
	}

	public class PropertyState<T> : IPropertyState
	{
		private readonly PropertyName m_propertyName;
		private readonly RegistryValueKind m_valueKind;
		private readonly Func<object, object> m_readConversion;
		private readonly Func<object, object> m_writeConversion;
		private readonly T m_default;

		private T m_value;

		public PropertyName Property
		{
			get { return m_propertyName; }
		}

		public RegistryValueKind ValueKind
		{
			get { return m_valueKind; }
		}

		public T Value
		{
			get
			{
				PropertyManager.GetProperty(this);
				return m_value;
			}
			set
			{
				m_value = value;
				PropertyManager.SetProperty(this);
			}
		}

		public PropertyState(PropertyName propertyName, RegistryValueKind valueKind, T defaultValue, Func<object, object> write, Func<object, object> read)
		{
			m_propertyName = propertyName;
			m_valueKind = valueKind;
			m_default = defaultValue;
			m_value = m_default;

			m_readConversion = read;
			m_writeConversion = write;
		}

		public object GetConvertedValue()
		{
			if (m_writeConversion != null)
			{
				return m_writeConversion(m_value);
			}
			else
			{
				return m_value;
			}
		}

		public void SetConvertedValue(object value)
		{
			if (m_readConversion != null)
			{
				object convert = m_readConversion(value);
				m_value = (T)Convert.ChangeType(convert, typeof(T));
			}
			else
			{
				m_value = (T)value;
			}
		}
	}
}
