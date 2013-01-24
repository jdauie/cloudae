using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using Jacere.Core.Attributes;

namespace Jacere.Core
{
	public static class ReflectionExtensions
	{
		public static Type[] GetTypes(this IEnumerable<Assembly> assemblies)
		{
			return assemblies
				.SelectMany(a => a.GetTypesSafely())
				.OrderBy(t => t.FullName)
				.ToArray();
		}

		public static ProductExtensionAttribute GetExtensionAttribute(this Assembly assembly)
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
				return (attributes.Single(a => a is ProductExtensionAttribute) as ProductExtensionAttribute);
			}

			return null;
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
