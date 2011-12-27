using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;

namespace CloudAE.Core
{
	public static class Context
	{
		static Context()
		{
		}

		public static void SaveWindowState(ISerializeStateBinary window)
		{
			SaveState(window, "Window");
		}

		public static void LoadWindowState(ISerializeStateBinary window)
		{
			LoadState(window, "Window");
		}

		private static void SaveState(ISerializeStateBinary state, string prefix)
		{
			PropertyManager.SetProperty(CreatePathPrefix(prefix) + state.GetIdentifier(), state);
		}

		private static void LoadState(ISerializeStateBinary state, string prefix)
		{
			PropertyManager.GetProperty(CreatePathPrefix(prefix) + state.GetIdentifier(), state);
		}

		private static string CreatePathPrefix(string prefix)
		{
			if (!string.IsNullOrWhiteSpace(prefix))
				prefix = prefix.Trim() + @"\";
			else
				prefix = "";

			return prefix;
		}
	}
}
