using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CloudAE.Core.Attributes
{
	[AttributeUsage(AttributeTargets.Assembly)]
	public class ProductExtensionAttribute : Attribute
	{
		private readonly string m_productName;

		public string ProductName
		{
			get { return m_productName; }
		}

		public ProductExtensionAttribute(string productName)
		{
			m_productName = productName;
		}
	}
}
