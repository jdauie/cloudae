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
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.ComponentModel;

using Microsoft.Win32;

using CloudAE.Core.Windows;

namespace CloudAE.Core
{
	public static class Context
	{
		public enum OptionCategory
		{
			Tiling = 1,
			Preview2D,
			Preview3D,
			App
		}

		private const string SETTINGS_TYPE_WINDOWS = "Windows";

		private static readonly Dictionary<PropertyName, IPropertyState> c_registeredProperties;
		private static readonly List<IPropertyState> c_registeredPropertiesList;

		private static readonly Dictionary<string, string> c_fileIndex;

		private static readonly string c_baseDirectory;
		private static readonly Type[] c_loadedTypes;

		private static Action<string, object[]> c_writeLineAction;

		private static Dictionary<string, FileHandlerBase> c_loadedPaths;
		private static Dictionary<PointCloudTileSource, FileHandlerBase> c_sources;
		private static ProcessingQueue c_queue;
		private static ManagedBackgroundWorker c_backgroundWorker;
		private static bool c_isProcessing;

		static Context()
		{
			Config.ContextStarted = true;

			Stopwatch stopwatch = new Stopwatch();
			stopwatch.Start();

			// this should go somewhere on startup
			// also, verify any other platform specs
			if (!BitConverter.IsLittleEndian)
			{
				throw new NotSupportedException();
			}

			AppDomain appDomain = AppDomain.CurrentDomain;

			c_baseDirectory = appDomain.BaseDirectory;

			if (Config.ShowConsole)
			{
				WinConsole.Initialize();
				c_writeLineAction = WinConsole.WriteLine;
			}
			else
			{
				c_writeLineAction = delegate(string s, object[] args) { Trace.WriteLine(string.Format(s, args)); };
			}

			c_registeredProperties = new Dictionary<PropertyName, IPropertyState>();
			c_registeredPropertiesList = new List<IPropertyState>();
			c_fileIndex = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
			c_sources = new Dictionary<PointCloudTileSource, FileHandlerBase>();
			c_loadedPaths = new Dictionary<string, FileHandlerBase>(StringComparer.OrdinalIgnoreCase);
			c_queue = new ProcessingQueue();

			c_backgroundWorker = new ManagedBackgroundWorker();
			c_backgroundWorker.WorkerReportsProgress = true;
			c_backgroundWorker.WorkerSupportsCancellation = true;
			c_backgroundWorker.DoWork += OnBackgroundDoWork;
			c_backgroundWorker.ProgressChanged += OnBackgroundProgressChanged;
			c_backgroundWorker.RunWorkerCompleted += OnBackgroundRunWorkerCompleted;

			Config.Write();

			if (Config.EnableExtensionDiscovery)
				RegisterExtensions();
			
			c_loadedTypes = appDomain.GetExtensionTypes(PropertyManager.PRODUCT_NAME).ToArray();

			if (Config.EnableFactoryDiscovery)
				RegisterFactories();

			if (Config.EnablePropertyDiscovery)
				RegisterProperties();

			long startupElapsed = stopwatch.ElapsedMilliseconds;
			stopwatch.Restart();

			if (Config.EnableInstrumentation)
				SystemInfo.Write();

			stopwatch.Stop();
			Context.WriteLine("[Startup]");
			Context.WriteLine("  Discover   : {0}ms", startupElapsed);
			Context.WriteLine("  Instrument : {0}ms", stopwatch.ElapsedMilliseconds);
			Context.WriteLine("  Total      : {0}ms", stopwatch.ElapsedMilliseconds + startupElapsed);
			Context.WriteLine();
		}

		#region Events

		public delegate void LogHandler(string value);
		public delegate void ProcessingStartedHandler(FileHandlerBase inputHandler);
		public delegate void ProcessingCompletedHandler(PointCloudTileSource tileSource);
		public delegate void ProcessingProgressChangedHandler(int progressPercentage);

		public static event LogHandler Log;
		public static event ProcessingStartedHandler ProcessingStarted;
		public static event ProcessingCompletedHandler ProcessingCompleted;
		public static event ProcessingProgressChangedHandler ProcessingProgressChanged;

		private static void OnLog(string value)
		{
			LogHandler handler = Log;
			if (handler != null)
				handler(value);
		}

		private static void OnProcessingStarted(FileHandlerBase inputHandler)
		{
			c_isProcessing = true;

			ProcessingStartedHandler handler = ProcessingStarted;
			if (handler != null)
				handler(inputHandler);
		}

		private static void OnProcessingCompleted(PointCloudTileSource tileSource)
		{
			c_isProcessing = false;

			ProcessingCompletedHandler handler = ProcessingCompleted;
			if (handler != null)
				handler(tileSource);
		}

		private static void OnProcessingProgressChanged(int progressPercentage)
		{
			ProcessingProgressChangedHandler handler = ProcessingProgressChanged;
			if (handler != null)
				handler(progressPercentage);
		}

		#endregion

		#region Properties

		public static Type[] LoadedTypes
		{
			get { return c_loadedTypes; }
		}

		public static List<IPropertyState> RegisteredProperties
		{
			get { return c_registeredPropertiesList; }
		}

		public static ProcessingQueue ProcessingQueue
		{
			get { return c_queue; }
		}

		public static string BasePath
		{
			get { return c_baseDirectory; }
		}

		public static bool IsProcessing
		{
			get { return c_isProcessing; }
		}

		public static bool IsProcessingQueueEmpty
		{
			get { return (c_queue.Count == 0); }
		}

		public static bool HasTileSources
		{
			get { return (c_sources.Count > 0); }
		}

		#endregion

		#region BackgroundWorker

		private static void OnBackgroundDoWork(object sender, DoWorkEventArgs e)
		{
			FileHandlerBase inputHandler = e.Argument as FileHandlerBase;
			ProgressManager progressManager = new BackgroundWorkerProgressManager(c_backgroundWorker, e, inputHandler, OnLog);
			
			ProcessingSet processingSet = new ProcessingSet(inputHandler);
			PointCloudTileSource tileSource = processingSet.Process(progressManager);

			if (tileSource != null)
			{
				tileSource.GeneratePreviewGrid(progressManager);

				e.Result = tileSource;
			}
		}

		private static void OnBackgroundRunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
		{
			FileHandlerBase inputHandler = (sender as ManagedBackgroundWorker).Manager.UserState as FileHandlerBase;
			PointCloudTileSource tileSource = null;

			if ((e.Cancelled == true))
			{
				OnProcessingProgressChanged(0);
			}
			else if (!(e.Error == null)) { }
			else
			{
				// success
				tileSource = e.Result as PointCloudTileSource;
			}

			if (tileSource != null)
				AddTileSource(tileSource, inputHandler);
			else
				c_loadedPaths.Remove(inputHandler.FilePath);

			OnProcessingCompleted(tileSource);

			StartNextInProcessingQueue();
		}

		private static void OnBackgroundProgressChanged(object sender, ProgressChangedEventArgs e)
		{
			OnProcessingProgressChanged(e.ProgressPercentage);
		}

		#endregion

		#region TileSources

		private static void AddToProcessingQueue(string path)
		{
			if (!File.Exists(path))
				throw new FileNotFoundException("Item cannot be added to queue.", path);

			FileHandlerBase handler = HandlerFactory.GetInputHandler(path);
			if (handler != null)
			{
				c_queue.Enqueue(handler);
				c_loadedPaths.Add(handler.FilePath, handler);
			}

			StartNextInProcessingQueue();
		}

		public static void ClearProcessingQueue(bool cancelCurrentProcessing)
		{
			foreach (string path in c_queue.Select(h => h.FilePath))
				c_loadedPaths.Remove(path);
			c_queue.Clear();

			if (cancelCurrentProcessing)
			{
				if (IsProcessing)
					c_backgroundWorker.CancelAsync();
			}
		}

		public static void AddToProcessingQueue(string[] paths)
		{
			List<string> skipped = new List<string>();
			foreach (string path in paths.OrderBy(p => p))
			{
				if (c_loadedPaths.ContainsKey(path))
					skipped.Add(path);
				else
					AddToProcessingQueue(path);
			}

			// maybe show a dialog or highlight the first skipped entry 
			// if it is already in sources and none of the others were valid
			if (skipped.Count > 0)
			{
				Context.WriteLine();
				Context.WriteLine("[Skipped]");
				foreach (string path in skipped)
					Context.WriteLine("  {0}", path);
			}
		}

		private static void StartNextInProcessingQueue()
		{
			if (c_queue.Count > 0)
			{
				if (!c_backgroundWorker.IsBusy)
				{
					FileHandlerBase inputHandler = c_queue.Dequeue();
					OnProcessingStarted(inputHandler);
					c_backgroundWorker.RunWorkerAsync(inputHandler);
				}
			}
		}

		public static void Remove(PointCloudTileSource tileSource)
		{
			if (c_sources.ContainsKey(tileSource))
			{
				c_loadedPaths.Remove(c_sources[tileSource].FilePath);
				c_sources.Remove(tileSource);
			}
		}

		public static void RemoveAll()
		{
			c_sources.Clear();
		}

		private static void AddTileSource(PointCloudTileSource tileSource, FileHandlerBase inputHandler)
		{
			c_sources.Add(tileSource, inputHandler);
		}

		#endregion

		public static void WriteLine()
		{
			WriteLine("");
		}

		public static void WriteLine(string value, params object[] args)
		{
			if (c_writeLineAction != null)
				c_writeLineAction(value, args);
		}

		public static IEnumerable<Type> GetLoadedTypes(Func<Type, bool> predicate)
		{
			return c_loadedTypes.Where(predicate);
		}

		public static string GetFilePath(string file)
		{
			if (c_fileIndex.ContainsKey(file))
				return c_fileIndex[file];
			return null;
		}

		#region Discovery

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
				string[] files = Directory.GetFiles(path, "*", SearchOption.AllDirectories);

				foreach (string file in files)
				{
					string key = Path.GetFileName(file);
					if (!c_fileIndex.ContainsKey(key))
						c_fileIndex.Add(key, file);
				}

				var newAssemblyPaths = files
					.Where(f => Path.GetExtension(f) == ".dll" && !assemblyLookup.ContainsKey(f));

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
					Context.WriteLine(" {0} {1}", result, Path.GetFileNameWithoutExtension(assemblyPath));
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

				if (Config.ShowAbstractTypesDuringDiscovery || result != '-')
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
						valueKind = RegistryValueKind.DWord;
						break;
					case TypeCode.Int64:
						valueKind = RegistryValueKind.QWord;
						break;
					case TypeCode.UInt32:
					case TypeCode.Single:
					case TypeCode.UInt64:
					case TypeCode.Double:
						valueKind = RegistryValueKind.Binary;
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
						readConversion  = (value => ((int)value == 1));
						writeConversion = (value => (int)((bool)value ? 1 : 0));
						break;
					case TypeCode.Byte:
					case TypeCode.SByte:
					case TypeCode.Int16:
					case TypeCode.UInt16:
						readConversion  = (value => (int)value);
						writeConversion = (value => Convert.ToInt32(value));
						break;
					case TypeCode.UInt32:
						readConversion  = (value => BitConverter.ToUInt32((byte[])value, 0));
						writeConversion = (value => BitConverter.GetBytes((uint)value));
						break;
					case TypeCode.UInt64:
						readConversion  = (value => (long)value);
						writeConversion = (value => Convert.ToInt64(value));
						break;
					case TypeCode.Single:
						readConversion  = (value => BitConverter.ToSingle((byte[])value, 0));
						writeConversion = (value => BitConverter.GetBytes((float)value));
						break;
					case TypeCode.Double:
						readConversion  = (value => BitConverter.ToDouble((byte[])value, 0));
						writeConversion = (value => BitConverter.GetBytes((double)value));
						break;
				}

				state = new PropertyState<T>(propertyName, valueKind, defaultValue, writeConversion, readConversion);
				c_registeredProperties.Add(propertyName, state);
				c_registeredPropertiesList.Add(state);
			}

			return state;
		}

		#endregion

		public static void Startup()
		{
			// calling this triggers the class constructor, so 
			// this is actually happening after the constructor
		}

		public static void Shutdown()
		{
			WinConsole.DestroyConsole();
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
