using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Jacere.Core
{
	public class Parameter<T>
	{
		private readonly ParameterDefinition<T> m_definition;
		private readonly T m_value;

		public static ParameterDefinition<T> Define(string name, T defaultValue)
		{
			return new ParameterDefinition<T>(name, defaultValue);
		}

		public Parameter(ParameterDefinition<T> definition, T value)
		{
			m_definition = definition;
		}
	}

	public class ParameterDefinition<T>
	{
		private readonly string m_name;
		private readonly Type m_type;
		private readonly T m_default;

		public ParameterDefinition(string name, T defaultValue)
		{
			m_name = name;
			m_default = defaultValue;
			m_type = typeof(T);
		}
	}
}
