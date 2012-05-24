using Microsoft.Win32;
using System.ComponentModel;

namespace CloudAE.Core
{
	public interface IPropertyState : INotifyPropertyChanged
	{
		PropertyName Property { get; }
		RegistryValueKind ValueKind { get; }

		object GetConvertedValue();
		void SetConvertedValue(object value);
	}
}
