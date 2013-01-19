using Microsoft.Win32;
using System.ComponentModel;

namespace Jacere.Core
{
	public interface IPropertyState : INotifyPropertyChanged
	{
		PropertyName Property { get; }

		object GetConvertedValue();
		void SetConvertedValue(object value);
	}

	public interface IPropertyState<T> : IPropertyState
	{
		T Value { get; set; }
	}
}
