using System;
using System.IO;
using System.Linq;
using Microsoft.Win32;

namespace CloudAE.Core
{
	public class RegistryPropertyManager : IPropertyManager
	{
		public IPropertyState<T> Create<T>(PropertyName propertyName, T defaultValue)
		{
			Type actualType = typeof(T);
			Type type = actualType.IsEnum ? Enum.GetUnderlyingType(actualType) : actualType;

			TypeCode typeCode = Type.GetTypeCode(type);

			RegistryValueKind valueKind = RegistryValueKind.None;
			Func<object, object> writeConversion = null;
			Func<object, object> readConversion = null;
			switch (typeCode)
			{
				case TypeCode.Boolean:
				case TypeCode.Byte:
				case TypeCode.SByte:
				case TypeCode.Int16:
				case TypeCode.UInt16:
				case TypeCode.Int32:
					valueKind = RegistryValueKind.DWord;
					break;
				case TypeCode.Int64:
					valueKind = RegistryValueKind.QWord;
					break;
				case TypeCode.UInt32:
				case TypeCode.Single:
				case TypeCode.UInt64:
				case TypeCode.Double:
					valueKind = RegistryValueKind.Binary;
					break;
				case TypeCode.String:
					valueKind = RegistryValueKind.String;
					break;
				default:
					throw new InvalidOperationException("Unsupported property type.");
			}

			switch (typeCode)
			{
				case TypeCode.Boolean:
					readConversion = (value => ((int)value == 1));
					writeConversion = (value => (int)((bool)value ? 1 : 0));
					break;
				case TypeCode.Byte:
				case TypeCode.SByte:
				case TypeCode.Int16:
				case TypeCode.UInt16:
					readConversion = (value => (int)value);
					writeConversion = (value => Convert.ToInt32(value));
					break;
				case TypeCode.UInt32:
					readConversion = (value => BitConverter.ToUInt32((byte[])value, 0));
					writeConversion = (value => BitConverter.GetBytes((uint)value));
					break;
				case TypeCode.UInt64:
					readConversion = (value => (long)value);
					writeConversion = (value => Convert.ToInt64(value));
					break;
				case TypeCode.Single:
					readConversion = (value => BitConverter.ToSingle((byte[])value, 0));
					writeConversion = (value => BitConverter.GetBytes((float)value));
					break;
				case TypeCode.Double:
					readConversion = (value => BitConverter.ToDouble((byte[])value, 0));
					writeConversion = (value => BitConverter.GetBytes((double)value));
					break;
			}

			return new RegistryPropertyState<T>(propertyName, valueKind, defaultValue, writeConversion, readConversion);
		}

		public PropertyName CreatePropertyName(string name)
		{
			string[] names = name.Split(new char[] { '\\' }, StringSplitOptions.RemoveEmptyEntries);
			string valueName = names[names.Length - 1];

			string subKeyPath = PropertyManager.APP_DATA_KEY;
			for (int i = 0; i < names.Length - 1; i++)
				subKeyPath += @"\" + names[i];

			string identifier = String.Join<string>(@"\", names);

			return new PropertyName(identifier, subKeyPath, valueName);
		}

		public PropertyName CreatePropertyName(string prefix, string name)
		{
			if (!string.IsNullOrWhiteSpace(prefix))
				name = string.Format("{0}{1}{2}", prefix.Trim(), @"\", name);

			return CreatePropertyName(name);
		}

		#region ISerializeStateBinary Handlers

		public bool SetProperty(PropertyName property, ISerializeStateBinary value)
		{
			return WriteKey(property, RegistryValueKind.Binary, () => {
				var ms = new MemoryStream();
				using (var writer = new BinaryWriter(ms))
				{
					writer.Write(value);
					writer.Flush();
					return ms.ToArray();
				}
			});
		}

		public bool GetProperty(PropertyName property, ISerializeStateBinary value)
		{
			return ReadKey(property, o => {
				byte[] bytes = o as byte[];
				if (bytes != null)
				{
					var ms = new MemoryStream(bytes);
					using (var reader = new BinaryReader(ms))
						value.Deserialize(reader);
				}
			});
		}

		#endregion

		#region IPropertyState Handlers

		public bool SetProperty(IPropertyState state)
		{
			var regState = state as IRegistryPropertyState;
			if (regState == null)
				throw new ArgumentException("state");

			return WriteKey(state.Property, regState.ValueKind, state.GetConvertedValue);
		}

		public bool GetProperty(IPropertyState state)
		{
			var regState = state as IRegistryPropertyState;
			if (regState == null)
				throw new ArgumentException("state");

			return ReadKey(state.Property, state.SetConvertedValue);
		}

		#endregion

		#region Helpers

		private static bool WriteKey(PropertyName property, RegistryValueKind valueKind, Func<object> process)
		{
			bool success = false;

			try
			{
				using (RegistryKey hiveKey = Registry.CurrentUser)
				{
					using (RegistryKey subKey = hiveKey.CreateSubKey(property.Path))
					{
						if (subKey != null)
						{
							subKey.SetValue(property.Name, process(), valueKind);
							success = true;
						}
					}
				}
			}
			catch { }

			return success;
		}

		private static bool ReadKey(PropertyName property, Action<object> process)
		{
			bool success = false;

			try
			{
				using (RegistryKey hiveKey = Registry.CurrentUser)
				{
					using (RegistryKey subKey = hiveKey.OpenSubKey(property.Path))
					{
						if (subKey != null)
						{
							object value = subKey.GetValue(property.Name);
							if (value != null)
							{
								process(value);
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
}
