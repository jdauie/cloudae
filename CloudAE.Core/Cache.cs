using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.IO;

namespace CloudAE.Core
{
	public static class Cache
	{
		public static readonly string APP_CACHE_DIR;
		public static readonly DriveInfo APP_CACHE_DRIVE;

		static Cache()
		{
			APP_CACHE_DIR = Path.Combine(PropertyManager.APP_TEMP_DIR, "cache");
			APP_CACHE_DRIVE = new DriveInfo(Path.GetPathRoot(APP_CACHE_DIR));
		}

		public static long CacheSize
		{
			get
			{
				long size = 0;
				if (Directory.Exists(APP_CACHE_DIR))
				{
					string[] files = Directory.GetFiles(APP_CACHE_DIR, "*", SearchOption.AllDirectories);
					size = files.Select(f => new FileInfo(f).Length).Sum();
				}
				return size;
			}
		}

		public static bool Clear()
		{
			int deleted = 0;
			int locked = 0;
			int failed = 0;

			if (Directory.Exists(APP_CACHE_DIR))
			{
				string[] files = Directory.GetFiles(APP_CACHE_DIR, "*", SearchOption.AllDirectories);
				
				foreach (string file in files)
				{
					FileStream streamLock = null;
					try
					{
						streamLock = File.Open(file, FileMode.Open, FileAccess.Write, FileShare.None);
					}
					catch
					{
						++locked;
					}

					if (streamLock != null)
					{
						streamLock.Dispose();
						try
						{
							File.Delete(file);
							++deleted;
						}
						catch
						{
							++failed;
						}
					}
				}
			}

			Context.WriteLine("Cache.Clear: {0} deleted, {1} locked, {2} failed", deleted, locked, failed);

			return (failed == 0);
		}
	}
}
