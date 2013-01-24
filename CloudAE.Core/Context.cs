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
 * 
 * http://opensource.org/licenses/ISC
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.ComponentModel;

using Jacere.Core;
using Jacere.Core.Util;
using Jacere.Core.Windows;
using Jacere.Data.PointCloud;

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

		private static readonly Action<string, object[]> c_writeLineAction;

		private static readonly Dictionary<string, FileHandlerBase> c_loadedPaths;
		private static readonly Dictionary<PointCloudTileSource, FileHandlerBase> c_sources;
		private static readonly ProcessingQueue c_queue;
		private static readonly ManagedBackgroundWorker c_backgroundWorker;
		
		private static bool c_isProcessing;

		static Context()
		{
			Config.ContextStarted = true;

			var stopwatch = new Stopwatch();
			stopwatch.Start();

			// this should go somewhere on startup
			// also, verify any other platform specs
			if (!BitConverter.IsLittleEndian)
			{
				throw new NotSupportedException();
			}

			if (Config.ShowConsole)
			{
				WinConsole.Initialize();
				c_writeLineAction = WinConsole.WriteLine;
			}
			else
			{
				c_writeLineAction = (s, args) => Trace.WriteLine(string.Format(s, args));
			}

			c_registeredProperties = new Dictionary<PropertyName, IPropertyState>();
			c_registeredPropertiesList = new List<IPropertyState>();
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


#warning EXTENSION SEARCH IS AUTOMATIC (for testing)
			System.Runtime.CompilerServices.RuntimeHelpers.RunClassConstructor(typeof(ExtensionManager).TypeHandle);
			//if (Config.EnableExtensionDiscovery)
			//    RegisterExtensions();

			if (Config.EnableFactoryDiscovery)
				RegisterFactories();

			if (Config.EnablePropertyDiscovery)
				RegisterProperties();

			long startupElapsed = stopwatch.ElapsedMilliseconds;
			stopwatch.Restart();

			if (Config.EnableInstrumentation)
				SystemInfo.Write();

			stopwatch.Stop();
			WriteLine("[Startup]");
			WriteLine("  Discover   : {0}ms", startupElapsed);
			WriteLine("  Instrument : {0}ms", stopwatch.ElapsedMilliseconds);
			WriteLine("  Total      : {0}ms", stopwatch.ElapsedMilliseconds + startupElapsed);
			WriteLine();
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

		public static List<IPropertyState> RegisteredProperties
		{
			get { return c_registeredPropertiesList; }
		}

		public static ProcessingQueue ProcessingQueue
		{
			get { return c_queue; }
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
			var inputHandler = e.Argument as FileHandlerBase;
			ProgressManager progressManager = new BackgroundWorkerProgressManager(c_backgroundWorker, e, inputHandler, OnLog);
			
			var processingSet = new ProcessingSet(inputHandler);
			var tileSource = processingSet.Process(progressManager);

			if (tileSource != null)
			{
				tileSource.GeneratePreviewGrid(progressManager);

				e.Result = tileSource;
			}
		}

		private static void OnBackgroundRunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
		{
			var worker = (sender as ManagedBackgroundWorker);
			if (worker == null)
				return;

			var inputHandler = worker.Manager.UserState as FileHandlerBase;
			if (inputHandler == null)
				return;

			PointCloudTileSource tileSource = null;

			if ((e.Cancelled))
			{
				OnProcessingProgressChanged(0);
			}
			else if (e.Error != null) { }
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

			var handler = HandlerFactory.GetInputHandler(path);
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
			if (paths == null || paths.Length == 0)
				return;

			var skipped = new List<string>();
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
				WriteLine();
				WriteLine("[Skipped]");
				foreach (string path in skipped)
					WriteLine("  {0}", path);
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
				tileSource.Close();
			}
		}

		public static void RemoveAll()
		{
			c_sources.Clear();
		}

		private static void AddTileSource(PointCloudTileSource tileSource, FileHandlerBase inputHandler)
		{
			tileSource.Open();
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

		#region Discovery

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
				baseType.IsAssignableFrom,
				t => !t.IsAbstract,
				t => System.Runtime.CompilerServices.RuntimeHelpers.RunClassConstructor(t.TypeHandle)
			);
		}

		public static void ProcessLoadedTypes(int level, string processName, Func<Type, bool> consider, Func<Type, bool> attempt, Action<Type> action)
		{
			string padding = "".PadRight(level * 2);

			WriteLine("{0}[{1}]", padding, processName);

			var types = ExtensionManager.GetLoadedTypes(consider);
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
					WriteLine("{0} {1} {2}", padding, result, type.Name);
			}
		}

		public static IPropertyState<T> RegisterOption<T>(OptionCategory category, string name, T defaultValue)
		{
			if (!Enum.IsDefined(typeof(OptionCategory), category))
				throw new ArgumentException("Invalid category.");

			if (string.IsNullOrWhiteSpace(name))
				throw new ArgumentException("Option registration is empty.", "name");

			if (name.IndexOfAny(new[] { '.', '\\', ' ', ':' }) > 0)
				throw new ArgumentException("Option registration contains invalid characters.", "name");

			string categoryName = Enum.GetName(typeof(OptionCategory), category);
			string optionName = String.Format("{0}.{1}", categoryName, name);

			IPropertyState<T> state = null;
			var propertyName = PropertyManager.CreatePropertyName(optionName);
			if (c_registeredProperties.ContainsKey(propertyName))
			{
				state = c_registeredProperties[propertyName] as IPropertyState<T>;
				if (state == null)
					throw new Exception("Duplicate option registration with a different type for {0}.");

				WriteLine("Duplicate option registration: ", propertyName);
			}
			else
			{
				state = PropertyManager.Create(propertyName, defaultValue);
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
			BufferManager.Shutdown();

			WinConsole.DestroyConsole();
		}

		#region Windows

		public static void SaveWindowState(ISerializeStateBinary window)
		{
			PropertyManager.SetProperty(window, SETTINGS_TYPE_WINDOWS);
		}

		public static void LoadWindowState(ISerializeStateBinary window)
		{
			PropertyManager.GetProperty(window, SETTINGS_TYPE_WINDOWS);
		}

		#endregion
	}
}
