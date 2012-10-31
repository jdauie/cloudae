using System;

namespace CloudAE.Core
{
	public interface IPropertyManager
	{
		PropertyName CreatePropertyName(string name);
		PropertyName CreatePropertyName(string prefix, string name);
		IPropertyState<T> Create<T>(PropertyName name, T defaultValue);
		bool SetProperty(PropertyName name, ISerializeStateBinary value);
		bool GetProperty(PropertyName name, ISerializeStateBinary value);
		bool SetProperty(IPropertyState state);
		bool GetProperty(IPropertyState state);
	}
}
