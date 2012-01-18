using Microsoft.Win32;

namespace CloudAE.Core
{
	public interface IPropertyState
	{
		PropertyName Property { get; }
		RegistryValueKind ValueKind { get; }

		object GetConvertedValue();
		void SetConvertedValue(object value);
	}
}
