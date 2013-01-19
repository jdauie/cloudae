using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;

using CloudAE.Core.Windows;

namespace Jacere.Core.Util
{
	public static class PathUtil
	{
		/// <summary>
		/// Return the sector size for the volume containing the specified path.
		/// </summary>
		/// <param name="path">UNC path name</param>
		/// <returns>Device sector size (bytes)</returns>
		public static uint GetDriveSectorSize(string path)
		{
			uint size;
			uint ignore;
			NativeMethods.GetDiskFreeSpace(Path.GetPathRoot(path), out ignore, out size, out ignore, out ignore);
			return size;
		}

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
				int error = NativeMethods.WNetGetConnection(driveRoot, sb, ref size);
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