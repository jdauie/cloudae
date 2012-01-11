using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using CloudAE.Core.Attributes;

namespace CloudAE.Core
{
	public static class ReflectionExtensions
	{
		public static Dictionary<string, Assembly> GetAssemblyLocationLookup(this AppDomain appDomain)
		{
			return appDomain
				.GetAssemblies()
				.Where(a => !String.IsNullOrEmpty(a.Location))
				.Distinct(a => a.Location, StringComparer.OrdinalIgnoreCase)
				.ToDictionary(a => a.Location, a => a, StringComparer.OrdinalIgnoreCase);
		}

		public static IEnumerable<Type> GetExtensionTypes(this AppDomain appDomain, string productName)
		{
			return appDomain
				.GetAssemblies()
				.Where(a => !string.IsNullOrEmpty(a.Location) && a.Location.StartsWith(appDomain.BaseDirectory, StringComparison.OrdinalIgnoreCase))
				.Where(a => a.IsExtensionAssembly(productName))
				.SelectMany(a => a.GetTypesSafely())
				.OrderBy(t => t.FullName);
		}

		public static bool IsExtensionAssembly(this Assembly assembly, string productName)
		{
			object[] attributes = null;
			try
			{
				attributes = assembly.GetCustomAttributes(typeof(ProductExtensionAttribute), false);
			}
			catch
			{
				// failed to load dependencies
			}

			if (attributes != null && attributes.Length == 1)
			{
				ProductExtensionAttribute extensionAttribute = attributes[0] as ProductExtensionAttribute;
				if (extensionAttribute != null)
					return extensionAttribute.ProductName.Equals(productName);
			}

			return false;
		}

		public static IEnumerable<Type> GetTypesSafely(this Assembly assembly)
		{
			try
			{
				return assembly.GetTypes();
			}
			catch (ReflectionTypeLoadException ex)
			{
				return ex.Types.Where(x => x != null);
			}
		}
	}

}
