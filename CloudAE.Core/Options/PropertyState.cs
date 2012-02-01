using System;
using Microsoft.Win32;

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
				m_value = value;
				m_hasValue = true;
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
	}
}
