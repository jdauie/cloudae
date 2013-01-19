using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Diagnostics;
using System.Text.RegularExpressions;
using Microsoft.Win32;

namespace CloudAE.Core
{
	public class HandlerFactory : IFactory
	{
		private static readonly List<IHandlerCreator> c_creators;
		private static readonly string c_filter;

		static HandlerFactory()
		{
			c_creators = RegisterCreators();
			c_filter = GetFilterString();
		}

		private static string GetFilterString()
		{
			List<string> filters = new List<string>();

			foreach (IHandlerCreator creator in c_creators)
			{
				string extensions = string.Join<string>(";", creator.SupportedExtensions.Select(e => string.Format("*.{0}", e)));
				filters.Add(string.Format("{0} files ({1})|{1}", creator.HandlerName, extensions));
			}

			filters.Add("All files (*.*)|*.*");

			return String.Join<string>("|", filters);
		}

		public static FileHandlerBase GetInputHandler(string path)
		{
			FileHandlerBase inputHandler = null;
			string extension = Path.GetExtension(path);
			
			foreach (IHandlerCreator creator in c_creators)
			{
				if (creator.IsSupportedExtension(extension))
				{
					inputHandler = creator.Create(path);
				}
			}

			return inputHandler;
		}

		private static List<IHandlerCreator> RegisterCreators()
		{
			List<IHandlerCreator> creators = new List<IHandlerCreator>();
			Type baseType = typeof(IHandlerCreator);

			Context.ProcessLoadedTypes(
				1,
				"Handlers",
				baseType.IsAssignableFrom,
				t => !t.IsAbstract,
				t => creators.Add(Activator.CreateInstance(t) as IHandlerCreator)
			);

			return creators;
		}

		public static OpenFileDialog GetOpenDialog()
		{
			OpenFileDialog dialog = new OpenFileDialog();
			dialog.Filter = c_filter;
			dialog.Multiselect = true;

			return dialog;
		}
	}
}
