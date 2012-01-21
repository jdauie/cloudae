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

		static Cache()
		{
			APP_CACHE_DIR = Path.Combine(PropertyManager.APP_TEMP_DIR, "cache");
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
	}
}
