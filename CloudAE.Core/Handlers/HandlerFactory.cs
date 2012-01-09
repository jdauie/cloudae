using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Diagnostics;

namespace CloudAE.Core
{
	public class HandlerFactory
	{
		private static List<FileHandlerBase> c_handlers;

		static HandlerFactory()
		{
			RegisterFactories();
		}

		public FileHandlerBase GetInputHandler(string path)
		{
			// this should go somewhere on startup
			if (!BitConverter.IsLittleEndian)
			{
				throw new NotSupportedException();
			}

			FileHandlerBase inputHandler = null;
			string extension = Path.GetExtension(path).ToLower();

			switch (extension)
			{
				case ".las":
					inputHandler = new LASFile(path);
					break;

				case ".xyz":
				case ".txt":
					inputHandler = new XYZFile(path);
					break;
			}

			return inputHandler;
		}

		private static void RegisterFactories()
		{
			Console.WriteLine("Registering Handlers...");

			c_handlers = new List<FileHandlerBase>();

			Type baseType = typeof(FileHandlerBase);
			AppDomain app = AppDomain.CurrentDomain;
			var assemblies = app.GetAssemblies();
			var factoryTypes = assemblies
				.SelectMany(a => a.GetTypes())
				.Where(t => baseType.IsAssignableFrom(t));

			foreach (Type type in factoryTypes)
			{
				char result = '-';
				if (!type.IsAbstract)
				{
					try
					{
						//ISourceFactory factory = Activator.CreateInstance(type, this) as ISourceFactory;
						//factory.Init();
						//m_factories.Add(factory);
						result = '+';
					}
					catch (Exception)
					{
						result = 'x';
					}
				}
				Console.WriteLine(" {0} {1}", result, type.Name);
			}
		}
	}
}
