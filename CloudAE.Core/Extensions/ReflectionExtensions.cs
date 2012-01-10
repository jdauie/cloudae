using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

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

		public static IEnumerable<Type> GetNestedTypes(this AppDomain appDomain)
		{
			return appDomain
				.GetAssemblies()
				.Where(a => !string.IsNullOrEmpty(a.Location) && a.Location.StartsWith(appDomain.BaseDirectory, StringComparison.OrdinalIgnoreCase))
				.SelectMany(a => a.GetTypesSafely())
				.OrderBy(t => t.FullName);
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
