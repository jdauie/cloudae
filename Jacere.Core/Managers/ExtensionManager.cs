using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.ComponentModel;

namespace Jacere.Core
{
	public static class ExtensionManager
	{
		private static readonly string c_baseDirectory;

		static ExtensionManager()
		{
			var appDomain = AppDomain.CurrentDomain;

			c_baseDirectory = appDomain.BaseDirectory;

			//RegisterExtensions();
		}

		#region Discovery

		///// <summary>
		///// Currently, this looks at all directories below the current domain base, recursively.
		///// </summary>
		//private static void RegisterExtensions()
		//{
		//    ContextManager.WriteLine("[Extensions]");

		//    var assemblyLookup = AppDomain.CurrentDomain.GetAssemblyLocationLookup();

		//    String path = c_baseDirectory;
		//    if (Directory.Exists(path))
		//    {
		//        var files = Directory.GetFiles(path, "*", SearchOption.AllDirectories);

		//        foreach (string file in files)
		//        {
		//            string key = Path.GetFileName(file);
		//            if (!c_fileIndex.ContainsKey(key))
		//                c_fileIndex.Add(key, file);
		//        }

		//        var newAssemblyPaths = files
		//            .Where(f => Path.GetExtension(f) == ".dll" && !assemblyLookup.ContainsKey(f));

		//        foreach (String assemblyPath in newAssemblyPaths)
		//        {
		//            char result = '-';
		//            try
		//            {
		//                PEHeader header = PEHeader.Load(assemblyPath);
		//                if (header.IsManaged)
		//                {
		//                    // figure a way to check this properly in the future
		//                    // (taking into account "Any CPU", etc.)
		//                    if (header.Is64Bit == Environment.Is64BitProcess)
		//                    {
		//                        AssemblyName assemblyName = AssemblyName.GetAssemblyName(assemblyPath);
		//                        Assembly assembly = Assembly.Load(assemblyName);
		//                        assemblyLookup.Add(assemblyPath, assembly);
		//                        result = '+';
		//                    }
		//                }
		//            }
		//            catch (Exception)
		//            {
		//                result = 'x';
		//            }
		//            ContextManager.WriteLine(" {0} {1}", result, Path.GetFileNameWithoutExtension(assemblyPath));
		//        }
		//    }
		//}

		//private static void RegisterFactories()
		//{
		//    ProcessLoadedTypesInitialize("Factories", typeof(IFactory));
		//}

		//private static void RegisterProperties()
		//{
		//    ProcessLoadedTypesInitialize("Properties", typeof(IPropertyContainer));
		//}

		//private static void ProcessLoadedTypesInitialize(string processName, Type baseType)
		//{
		//    ProcessLoadedTypes(
		//        0,
		//        processName,
		//        baseType.IsAssignableFrom,
		//        t => !t.IsAbstract,
		//        t => System.Runtime.CompilerServices.RuntimeHelpers.RunClassConstructor(t.TypeHandle)
		//    );
		//}

		//public static void ProcessLoadedTypes(int level, string processName, Func<Type, bool> consider, Func<Type, bool> attempt, Action<Type> action)
		//{
		//    string padding = "".PadRight(level * 2);

		//    WriteLine("{0}[{1}]", padding, processName);

		//    var types = GetLoadedTypes(consider);
		//    foreach (Type type in types)
		//    {
		//        char result = '-';
		//        if (attempt(type))
		//        {
		//            try
		//            {
		//                action(type);
		//                result = '+';
		//            }
		//            catch (Exception)
		//            {
		//                result = 'x';
		//            }
		//        }

		//        if (Config.ShowAbstractTypesDuringDiscovery || result != '-')
		//            WriteLine("{0} {1} {2}", padding, result, type.Name);
		//    }
		//}

		#endregion
	}
}
