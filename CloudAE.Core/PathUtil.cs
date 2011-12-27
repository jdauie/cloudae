using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using System.IO;
using System.Net;

namespace CloudAE.Core
{
	public static class PathUtil
	{
		[DllImport("mpr.dll", CharSet = CharSet.Unicode, SetLastError = true)]
		public static extern int WNetGetConnection(
			[MarshalAs(UnmanagedType.LPTStr)] string localName,
			[MarshalAs(UnmanagedType.LPTStr)] StringBuilder remoteName,
			ref int length);
		
		/// <summary>
		/// Given a path, returns the UNC path or the original. (No exceptions
		/// are raised by this function directly). For example, "P:\2008-02-29"
		/// might return: "\\networkserver\Shares\Photos\2008-02-09"
		/// </summary>
		/// <param name="originalPath">The path to convert to a UNC Path</param>
		/// <returns>A UNC path. If a network drive letter is specified, the
		/// drive letter is converted to a UNC or network path. If the
		/// originalPath cannot be converted, it is returned unchanged.</returns>
		public static string GetUNCPath(string originalPath)
		{
			StringBuilder sb = new StringBuilder(512);
			int size = sb.Capacity;

			string driveRoot = GetDriveRootFromPath(originalPath);
			if (!string.IsNullOrEmpty(driveRoot))
			{
				int error = WNetGetConnection(driveRoot, sb, ref size);
				if (error == 0)
				{
					DirectoryInfo dir = new DirectoryInfo(originalPath);

					string path = Path.GetFullPath(originalPath).Substring(Path.GetPathRoot(originalPath).Length);
					return Path.Combine(sb.ToString().TrimEnd(), path);
				}
			}

			return originalPath;
		}

		public static string GetDriveRootFromPath(string path)
		{
			if (path.Length > 2 && path[1] == ':')
			{
				char c = path[0];
				if ((c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z'))
				{
					return path.Substring(0, 2);
				}
			}

			return null;
		}

		public static bool IsLocalPath(string path)
		{
			string fullPath = Path.GetFullPath(path);
			string pathRoot = Path.GetPathRoot(fullPath);

			// check for mapped drive
			pathRoot = PathUtil.GetUNCPath(pathRoot);
			
			// if the path is to a local drive, we are done
			string driveRoot = PathUtil.GetDriveRootFromPath(pathRoot);
			if (!string.IsNullOrEmpty(driveRoot))
				return true;

			// otherwise, check for loopback
			if (pathRoot.Length > 2)
			{
				string[] pathParts = pathRoot.Split(new char[] { '\\' }, StringSplitOptions.RemoveEmptyEntries);
				if (pathParts.Length > 0)
					return IsLocalHost(pathParts[0]);
			}
			
			return false;
		}

		private static bool IsLocalHost(string hostname)
		{
			IPAddress[] host;
			try { host = Dns.GetHostAddresses(hostname); }
			catch (Exception) { return false; }
			IPAddress[] local = Dns.GetHostAddresses(Dns.GetHostName());
			return host.Any(hostAddress => IPAddress.IsLoopback(hostAddress) || local.Contains(hostAddress));
		}
	}
}