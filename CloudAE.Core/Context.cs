/*
 * Copyright (c) 2011, Joshua Morey <josh@joshmorey.com>
 * 
 * Permission to use, copy, modify, and/or distribute this software for any
 * purpose with or without fee is hereby granted, provided that the above
 * copyright notice and this permission notice appear in all copies.
 * 
 * THE SOFTWARE IS PROVIDED "AS IS" AND THE AUTHOR DISCLAIMS ALL WARRANTIES
 * WITH REGARD TO THIS SOFTWARE INCLUDING ALL IMPLIED WARRANTIES OF
 * MERCHANTABILITY AND FITNESS. IN NO EVENT SHALL THE AUTHOR BE LIABLE FOR
 * ANY SPECIAL, DIRECT, INDIRECT, OR CONSEQUENTIAL DAMAGES OR ANY DAMAGES
 * WHATSOEVER RESULTING FROM LOSS OF USE, DATA OR PROFITS, WHETHER IN AN
 * ACTION OF CONTRACT, NEGLIGENCE OR OTHER TORTIOUS ACTION, ARISING OUT OF
 * OR IN CONNECTION WITH THE USE OR PERFORMANCE OF THIS SOFTWARE.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using Microsoft.Win32;
using System.Diagnostics;
using System.Reflection;
using System.IO;

namespace CloudAE.Core
{
	public static class Context
	{
		public enum OptionCategory
		{
			Tiling = 1,
			Preview2D,
			Preview3D
		}

		public const bool STORE_PROPERTY_REGISTRATION = true;

		// options registration?
		// convert/remove deprecated options

		// add property validators and descriptions

		// cache options

		private const string SETTINGS_TYPE_WINDOWS = "Windows";

		private static readonly Dictionary<PropertyName, IPropertyState> c_registeredProperties;
		private static readonly List<IPropertyState> c_registeredPropertiesList;

		//private static readonly List<IFactory> c_factoryList;

		private static readonly string c_baseDirectory;
		private static readonly Type[] c_loadedTypes;

		private static Action<string, object[]> c_writeLineAction;

		static Context()
		{
			Stopwatch stopwatch = new Stopwatch();
			stopwatch.Start();

			// this should go somewhere on startup
			// also, verify any other platform specs
			if (!BitConverter.IsLittleEndian)
			{
				throw new NotSupportedException();
			}

			AppDomain appDomain = AppDomain.CurrentDomain;

			{
				c_baseDirectory = appDomain.BaseDirectory;
				c_writeLineAction = Console.WriteLine;

				c_registeredProperties = new Dictionary<PropertyName, IPropertyState>();
				c_registeredPropertiesList = new List<IPropertyState>();
			}

			RegisterExtensions();
			c_loadedTypes = appDomain.GetExtensionTypes(PropertyManager.PRODUCT_NAME).ToArray();
			RegisterFactories();
			RegisterProperties();

			stopwatch.Stop();

			{
				Context.WriteLine("[{0}]", typeof(Context).FullName);
				Context.WriteLine("Base:    {0}", c_baseDirectory);
				Context.WriteLine("Types:   {0}", c_loadedTypes.Length);
				Context.WriteLine("Options: {0}", c_registeredPropertiesList.Count);
				Context.WriteLine("Time:    {0}ms", stopwatch.ElapsedMilliseconds);

				Context.WriteLine("[Options]");
				foreach (IPropertyState property in c_registeredPropertiesList)
					Context.WriteLine("{0}", property.ToString());
			}
		}

		public static List<IPropertyState> RegisteredProperties
		{
			get { return c_registeredPropertiesList; }
		}

		public static void WriteLine(string value, params object[] args)
		{
			c_writeLineAction(value, args);
		}

		public static IEnumerable<Type> GetLoadedTypes(Func<Type, bool> predicate)
		{
			return c_loadedTypes.Where(predicate);
		}

		/// <summary>
		/// Currently, this looks at all directories below the current domain base, recursively.
		/// </summary>
		private static void RegisterExtensions()
		{
			Context.WriteLine("[Extensions]");

			var assemblyLookup = AppDomain.CurrentDomain.GetAssemblyLocationLookup();

			String path = c_baseDirectory;
			if (Directory.Exists(path))
			{
				var files = Directory.GetFiles(path, "*.dll", SearchOption.AllDirectories);
				var newAssemblyPaths = files
					.Distinct(StringComparer.OrdinalIgnoreCase)
					.Where(f => !assemblyLookup.ContainsKey(f));

				foreach (String assemblyPath in newAssemblyPaths)
				{
					char result = '-';
					try
					{
						AssemblyName assemblyName = AssemblyName.GetAssemblyName(assemblyPath);
						Assembly assembly = Assembly.Load(assemblyName);
						assemblyLookup.Add(assemblyPath, assembly);
						result = '+';
					}
					catch (Exception)
					{
						result = 'x';
					}
					Context.WriteLine(" {0} {1}", result, assemblyPath);
				}
			}
		}

		private static void RegisterFactories()
		{
			ProcessLoadedTypesInitialize("Factories", typeof(IFactory));
		}

		private static void RegisterProperties()
		{
			ProcessLoadedTypesInitialize("Properties", typeof(IPropertyContainer));
		}

		private static void ProcessLoadedTypesInitialize(string processName, Type baseType)
		{
			ProcessLoadedTypes(
				0,
				processName,
				t => baseType.IsAssignableFrom(t),
				t => !t.IsAbstract,
				t => System.Runtime.CompilerServices.RuntimeHelpers.RunClassConstructor(t.TypeHandle)
			);
		}

		public static void ProcessLoadedTypes(int level, string processName, Func<Type, bool> consider, Func<Type, bool> attempt, Action<Type> action)
		{
			string padding = "".PadRight(level * 2);

			Context.WriteLine("{0}[{1}]", padding, processName);

			var types = GetLoadedTypes(consider);
			foreach (Type type in types)
			{
				char result = '-';
				if (attempt(type))
				{
					try
					{
						action(type);
						result = '+';
					}
					catch (Exception)
					{
						result = 'x';
					}
				}
				Context.WriteLine("{0} {1} {2}", padding, result, type.Name);
			}
		}

		public static PropertyState<T> RegisterOption<T>(OptionCategory category, string name, T defaultValue)
		{
			if (!Enum.IsDefined(typeof(OptionCategory), category))
				throw new ArgumentException("Invalid category.");

			if (string.IsNullOrWhiteSpace(name))
				throw new ArgumentException("Option registration is empty.", "name");

			if (name.IndexOfAny(new char[] { '.', '\\', ' ' }) > 0)
				throw new ArgumentException("Option registration contains invalid characters.", "name");

			string categoryName = Enum.GetName(typeof(OptionCategory), category);
			string optionName = String.Format("{0}.{1}", categoryName, name);

			PropertyState<T> state = null;
			PropertyName propertyName = PropertyManager.ParsePropertyName(optionName);
			if (c_registeredProperties.ContainsKey(propertyName))
			{
				state = c_registeredProperties[propertyName] as PropertyState<T>;
				if (state == null)
					throw new Exception("Duplicate option registration with a different type for {0}.");

				Debug.Assert(false, "Duplicate option registration");
				Context.WriteLine("Duplicate option registration: ", propertyName);
			}
			else
			{
				Type actualType = typeof(T);
				Type type = actualType.IsEnum ? type = Enum.GetUnderlyingType(actualType) : actualType;

				TypeCode typeCode = Type.GetTypeCode(type);

				RegistryValueKind valueKind = RegistryValueKind.None;
				Func<object, object> writeConversion = null;
				Func<object, object> readConversion = null;
				switch (typeCode)
				{
					case TypeCode.Boolean:
					case TypeCode.Byte:
					case TypeCode.SByte:
					case TypeCode.Int16:
					case TypeCode.UInt16:
					case TypeCode.Int32:
					case TypeCode.UInt32:
					case TypeCode.Single:
						valueKind = RegistryValueKind.DWord;
						break;
					case TypeCode.Int64:
					case TypeCode.UInt64:
					case TypeCode.Double:
						valueKind = RegistryValueKind.QWord;
						break;
					case TypeCode.String:
						valueKind = RegistryValueKind.String;
						break;
					default:
						throw new InvalidOperationException("Unsupported property type.");
				}

				switch (typeCode)
				{
					case TypeCode.Boolean:
						writeConversion = (value => (int)((bool)value ? 1 : 0));
						readConversion  = (value => ((int)value == 1));
						break;
					case TypeCode.Byte:
					case TypeCode.SByte:
					case TypeCode.Int16:
					case TypeCode.UInt16:
					case TypeCode.UInt32:
						readConversion  = (value => (int)value);
						writeConversion = (value => Convert.ToInt32(value));
						break;
					case TypeCode.UInt64:
						readConversion  = (value => (long)value);
						writeConversion = (value => Convert.ToInt64(value));
						break;
					case TypeCode.Single:
						writeConversion = (value => BitConverter.ToInt32(BitConverter.GetBytes((float)value), 0));
						readConversion  = (value => BitConverter.ToSingle(BitConverter.GetBytes((int)value), 0));
						break;
					case TypeCode.Double:
						writeConversion = (value => BitConverter.ToInt64(BitConverter.GetBytes((double)value), 0));
						readConversion  = (value => BitConverter.ToDouble(BitConverter.GetBytes((long)value), 0));
						break;
				}

				state = new PropertyState<T>(propertyName, valueKind, defaultValue, writeConversion, readConversion);
				c_registeredProperties.Add(propertyName, state);
				c_registeredPropertiesList.Add(state);
			}

			return state;
		}

		public static void Startup()
		{
		}

		public static void Shutdown()
		{
		}

		#region Windows

		public static void SaveWindowState(ISerializeStateBinary window)
		{
			SaveState(window, SETTINGS_TYPE_WINDOWS);
		}

		public static void LoadWindowState(ISerializeStateBinary window)
		{
			LoadState(window, SETTINGS_TYPE_WINDOWS);
		}

		#endregion

		#region Property Helpers

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

		#endregion
	}
}
