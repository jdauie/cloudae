using System;
using Microsoft.Win32;
using System.ComponentModel;

namespace CloudAE.Core
{
	public class PropertyState<T> : IPropertyState
	{
		private readonly PropertyName m_propertyName;
		private readonly RegistryValueKind m_valueKind;
		private readonly Func<object, object> m_readConversion;
		private readonly Func<object, object> m_writeConversion;
		private readonly T m_default;
		private readonly Type m_type;

		private bool m_hasValue;
		private T m_value;

		public PropertyName Property
		{
			get { return m_propertyName; }
		}

		public RegistryValueKind ValueKind
		{
			get { return m_valueKind; }
		}

		public Type Type
		{
			get { return m_type; }
		}

		public bool IsDefault
		{
			get { return m_default.Equals(m_value); }
		}

		public T Value
		{
			get
			{
				if (PropertyManager.GetProperty(this))
					m_hasValue = true;

				return m_value;
			}
			set
			{
				bool isDefault = IsDefault;

				m_value = value;
				m_hasValue = true;
				PropertyManager.SetProperty(this);

				OnPropertyChanged("Value");

				if (isDefault != IsDefault)
					OnPropertyChanged("IsDefault");
			}
		}

		public static PropertyState<T> Create(PropertyName propertyName, T defaultValue)
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

			return new PropertyState<T>(propertyName, valueKind, defaultValue, writeConversion, readConversion);
		}

		private PropertyState(PropertyName propertyName, RegistryValueKind valueKind, T defaultValue, Func<object, object> write, Func<object, object> read)
		{
			m_propertyName = propertyName;
			m_valueKind = valueKind;
			m_default = defaultValue;
			m_value = m_default;

			m_readConversion = read;
			m_writeConversion = write;

			m_type = typeof(T);

			if (Config.StorePropertyRegistration)
			{
				T storedValue = Value;
				if (!m_hasValue)
					Value = storedValue;
			}
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

		// this should eventually return false for invalid values (once I have delegates for that)
		public void SetConvertedValue(object value)
		{
			if (m_readConversion != null)
			{
				object convert = m_readConversion(value);

				if (m_type.IsEnum)
					m_value = (T)Enum.ToObject(m_type, convert);
				else
					m_value = (T)Convert.ChangeType(convert, m_type);
			}
			else
			{
				m_value = (T)value;
			}
		}

		public override string ToString()
		{
			// this hits the registry at present
			return String.Format("{0} = {1}", m_propertyName, Value);
		}

		#region INotifyPropertyChanged Members

		public event PropertyChangedEventHandler PropertyChanged;

		protected void OnPropertyChanged(string name)
		{
			PropertyChangedEventHandler handler = PropertyChanged;
			if (handler != null)
				handler(this, new PropertyChangedEventArgs(name));
		}

		#endregion
	}
}
