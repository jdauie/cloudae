using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Management;
using System.Reflection;
using System.Text.RegularExpressions;

using CloudAE.Core.Windows;

namespace CloudAE.Core
{
	public static class SystemInfo
	{
		[Flags]
		private enum DebugInfo
		{
			Environment = 1 << 0,
			Context     = 1 << 1,
			Process     = 1 << 2,
			Memory      = 1 << 3,
			Options     = 1 << 4,
			AllDrives   = 1 << 5
		}

		private const string DATETIME_FORMAT = "yyyy-MM-dd HH:mm:ss";

		private static readonly Process m_process;

		private static Dictionary<string, bool> m_initialSystemInfo;

		static SystemInfo()
		{
			m_process = Process.GetCurrentProcess();

			m_initialSystemInfo = null;
		}

		#region System Info Methods

		public static void Write()
		{
			DebugInfo flags =
				DebugInfo.Environment |
				DebugInfo.Memory |
				DebugInfo.Context |
				DebugInfo.Options |
				DebugInfo.Process;

			//flags |= DebugInfo.AllDrives;

			Write(flags);
		}

		private static void Write(DebugInfo debugFlags)
		{
			var templateLines = GetSystemInfoTemplate(debugFlags);
			var outputLines = templateLines.SelectMany(s => GenerateLines(s, debugFlags)).ToList();

			if (m_initialSystemInfo == null)
			{
				m_initialSystemInfo = outputLines.Distinct().ToDictionary(s => s, s => !LineIsHeader(s));
			}
			else
			{
				outputLines = outputLines.Where(s => !m_initialSystemInfo.ContainsKey(s) || !m_initialSystemInfo[s]).ToList();
				
				// remove section headers that have no content
				int lastHeaderIndex = -2;
				for (int i = 0; i < outputLines.Count; i++)
				{
					string line = outputLines[i];
					if (LineIsHeader(line))
					{
						if ((i - lastHeaderIndex) == 1)
						{
							outputLines.RemoveAt(lastHeaderIndex);
							--i;
							lastHeaderIndex = -2;
						}

						if (i == (outputLines.Count - 1))
							outputLines.RemoveAt(i);

						lastHeaderIndex = i;
					}
				}
			}

			foreach (string line in outputLines)
			{
				Context.WriteLine(line);
			}
		}

		private static List<string> GetVariableUsages(string templateLine)
		{
			Regex matchVariables = new Regex(@"\$\w+", RegexOptions.Compiled);
			MatchCollection matches = matchVariables.Matches(templateLine);

			return matches.Cast<Match>().Select(m => m.Value.Substring(1)).Distinct().ToList();
		}

		private static List<string> GenerateLines(string sourceLine, DebugInfo flags)
		{
			List<string> variables = GetVariableUsages(sourceLine);
			List<string> newLines = new List<string>();

			if (variables.Count == 0)
			{
				newLines.Add(sourceLine);
			}
			else
			{
				// check if one of them has a multi-line handler
				if (variables[0] == "DriveLetter")
				{
					string queryString = "SELECT Name FROM Win32_LogicalDisk";
					if ((flags & DebugInfo.AllDrives) == 0)
						queryString += " WHERE DriveType = 3";
					else
						queryString += " WHERE NOT DriveType = 5";
					SelectQuery query = new SelectQuery(queryString);
					ManagementObjectSearcher searcher = new ManagementObjectSearcher(query);
					var drives = searcher.Get().Cast<ManagementObject>().Select(o => new DriveInfo((string)o["Name"]));
					foreach (DriveInfo d in drives)
					{
						string newLine = sourceLine;
						newLine = ReplaceVariable(newLine, "DriveLetter", GetDriveName(d));
						newLine = ReplaceVariable(newLine, "DriveDetails", GetDriveDetails(d));
						newLines.Add(newLine);
					}
				}
				else if (variables[0] == "Option")
				{
					newLines.AddRange(Context.RegisteredProperties.Select(p => ReplaceVariable(sourceLine, "Option", p.ToString())));
				}
				else
				{
					string newLine = sourceLine;
					foreach (string variable in variables)
					{
						string value = GetVariableValue(variable);
						if (value != null)
							newLine = ReplaceVariable(newLine, variable, value);
					}
					newLines.Add(newLine);
				}
			}
			return newLines;
		}

		private static bool LineIsHeader(string line)
		{
			return (line.StartsWith("[") && line.EndsWith("]"));
		}

		private static string ReplaceVariable(string line, string variable, string value)
		{
			return line.Replace(string.Format("${0}", variable), value);
		}

		private static string GetVariableValue(string variableName)
		{
			try
			{
				switch (variableName)
				{
					case "User":                  return Environment.UserName;
					case "Domain":                return Environment.UserDomainName;
					case "Machine":               return Environment.MachineName;
					case "DebuggerAttached":      return Debugger.IsAttached.ToString();
					case "Interactive":           return Environment.UserInteractive.ToString();
					case "OS":                    return GetOSInfo();
					case "CLR":                   return Environment.Version.ToString();
					case "Processors":            return GetProcessorInfo();
					case "Graphics":              return GetVideoInfo();

					case "Types":                 return Context.LoadedTypes.Length.ToString();
					case "Options":               return Context.RegisteredProperties.Count.ToString();
					case "BasePath":              return Context.BasePath;
					case "Temp":                  return Cache.APP_CACHE_DIR;
					case "TempUsed":              return Cache.CacheSize.ToSize();

					case "ProcName":              return m_process.ProcessName;
					case "ProcId":                return m_process.Id.ToString();
					case "ProcStart":             return m_process.StartTime.ToString(DATETIME_FORMAT);
					case "ProcHandles":           return m_process.HandleCount.ToString();
					case "ProcTotalCpu":          return m_process.TotalProcessorTime.ToString();
					case "ProcUserCpu":           return m_process.UserProcessorTime.ToString();

					case "ProcWorkingSet":        return m_process.WorkingSet64.ToSize();
					case "PeakProcWorkingSet":    return m_process.PeakWorkingSet64.ToSize();
					case "ProcPagedMemory":       return m_process.PagedMemorySize64.ToSize();
					case "PeakProcPagedMemory":   return m_process.PeakPagedMemorySize64.ToSize();
					case "ProcVirtualMemory":     return m_process.VirtualMemorySize64.ToSize();
					case "PeakProcVirtualMemory": return m_process.PeakVirtualMemorySize64.ToSize();

					default: return null;
				}
			}
			catch { }

			return null;
		}

		private static List<string> GetSystemInfoTemplate(DebugInfo infoFlags)
		{
			List<string> templateLines = new List<string>();

			if ((infoFlags & DebugInfo.Environment) > 0)
			{
				templateLines.Add(@"[Environment]");
				templateLines.Add(@"  User       : $Domain\$User");
				templateLines.Add(@"  Machine    : $Machine");
				templateLines.Add(@"  Debugging  : $DebuggerAttached");
				templateLines.Add(@"  Interactive: $Interactive");
				templateLines.Add(@"  OS Version : $OS");
				templateLines.Add(@"  CLR Version: $CLR");
				templateLines.Add(@"  Processors : $Processors");
				templateLines.Add(@"  Graphics   : $Graphics");
				templateLines.Add(@"  Drive $DriveLetter    : $DriveDetails");
			}
			if ((infoFlags & DebugInfo.Context) > 0)
			{
				templateLines.Add(@"[Context]");
				templateLines.Add(@"  Types      : $Types");
				templateLines.Add(@"  Options    : $Options");
				templateLines.Add(@"  Base Path  : $BasePath");
				templateLines.Add(@"  Temp       : $Temp");
				templateLines.Add(@"  Temp Used  : $TempUsed");
			}
			if ((infoFlags & DebugInfo.Process) > 0)
			{
				templateLines.Add(@"[Process]");
				templateLines.Add(@"  Name       : $ProcName");
				templateLines.Add(@"  ID         : $ProcId");
				templateLines.Add(@"  Start Time : $ProcStart");
				templateLines.Add(@"  Handles    : $ProcHandles");
				templateLines.Add(@"  Total CPU  : $ProcTotalCpu");
				templateLines.Add(@"  User CPU   : $ProcUserCpu");
			}
			if ((infoFlags & DebugInfo.Memory) > 0)
			{
				templateLines.Add(@"[Memory]");
				templateLines.Add(@"  Working Set: $ProcWorkingSet ($PeakProcWorkingSet)");
				templateLines.Add(@"  Paged      : $ProcPagedMemory ($PeakProcPagedMemory)");
				templateLines.Add(@"  Virtual    : $ProcVirtualMemory ($PeakProcVirtualMemory)");
			}
			if ((infoFlags & DebugInfo.Options) > 0)
			{
				templateLines.Add(@"[Options]");
				templateLines.Add(@"  $Option");
			}

			return templateLines;
		}

		#endregion

		#region Instrumentation Functions

		private static string GetProcessorInfo()
		{
			SelectQuery query = new SelectQuery("SELECT Name, MaxClockSpeed, AddressWidth FROM Win32_Processor");
			using (ManagementObjectSearcher searcher = new ManagementObjectSearcher(query))
			{
				var cpu = searcher.Get().Cast<ManagementObject>().First();
				return string.Format("{0}, {1}, {2}Mhz, {3}-bit", Environment.ProcessorCount, (string)cpu["Name"], (uint)cpu["MaxClockSpeed"], (ushort)cpu["AddressWidth"]);
			}
		}

		private static string GetOSInfo()
		{
			SelectQuery query = new SelectQuery("SELECT Caption, Version, CSDVersion FROM Win32_OperatingSystem");
			using (ManagementObjectSearcher searcher = new ManagementObjectSearcher(query))
			{
				var os = searcher.Get().Cast<ManagementObject>().First();
				return string.Format("{0} ({1} {2})", (string)os["Caption"], (string)os["Version"], (string)os["CSDVersion"]);
			}
		}

		private static string GetVideoInfo()
		{
			SelectQuery query = new SelectQuery("SELECT Name, DriverVersion, AdapterRAM, VideoModeDescription FROM Win32_VideoController");
			ManagementObjectSearcher searcher = new ManagementObjectSearcher(query);
			var os = searcher.Get().Cast<ManagementObject>().First();

			return string.Format("{0}, {1}, {2}, {3}", (string)os["Name"], (string)os["DriverVersion"], ((uint)os["AdapterRAM"]).ToSize(), (string)os["VideoModeDescription"]); ;
		}

		#endregion

		#region Utility Functions

		private static void CopyStream(Stream input, Stream output)
		{
			byte[] b = new byte[32768];
			int r;
			while ((r = input.Read(b, 0, b.Length)) > 0)
				output.Write(b, 0, r);
		}

		private static string GetDriveName(DriveInfo d)
		{
			return (d != null ? d.Name.Replace(@":\", "") : "?");
		}

		private static string GetDriveDetails(DriveInfo d)
		{
			string driveDetails = string.Empty;
			if (d != null)
			{
				DriveType type = d.DriveType;
				driveDetails = type.ToString();
				if (type != DriveType.CDRom)
				{
					try
					{
						long free;
						long freeUser;
						long total;
						if (NativeMethods.GetDiskFreeSpaceEx(d.Name, out freeUser, out total, out free))
						{
							driveDetails += String.Format(" {0}, {1} total, {2} free", d.DriveFormat, total.ToSize(), free.ToSize());
							if (freeUser < free)
								driveDetails += String.Format(", {0} available", freeUser.ToSize());
							
							if (type == DriveType.Network)
							{
								string uncPath = PathUtil.GetUNCPath(d.Name);
								driveDetails += String.Format(", {0}", uncPath);
							}
						}
					}
					catch { }
				}
			}
			return driveDetails;
		}

		private static object GetProperty(object source, string property)
		{
			if (source != null)
			{
				Type type = source.GetType();
				MemberInfo memberInfo = type.GetProperty(property);
				if (memberInfo != null)
				{
					BindingFlags flags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.GetProperty;
					object result = type.InvokeMember(property, flags, null, source, null);
					return result;
				}
			}

			return null;
		}

		public static Stream GetDecompressedStream(Stream rawStream)
		{
			byte[] gzipSignature = new byte[] { 0x1F, 0x8B, 0x08 };
			bool isGZip = SignatureMatches(rawStream, gzipSignature);

			if (isGZip)
			{
				GZipStream compressedStream = new GZipStream(rawStream, CompressionMode.Decompress);
				return compressedStream;
			}

			return null;
		}

		private static Stream GetCompressedStream(Stream rawStream)
		{
			GZipStream compressedStream = new GZipStream(rawStream, CompressionMode.Compress, true);
			return compressedStream;
		}

		private static bool SignatureMatches(Stream rawStream, byte[] signature)
		{
			bool match = false;
			byte[] buffer = new byte[signature.Length];
			if (rawStream.Length > buffer.Length)
			{
				rawStream.Read(buffer, 0, buffer.Length);
				rawStream.Position = 0;

				match = true;
				for (int i = 0; i < buffer.Length; i++)
				{
					if (buffer[i] != signature[i])
					{
						match = false;
						break;
					}
				}
			}
			return match;
		}

		private static string ReplaceSymbols(string value)
		{
			Dictionary<string, string> replacements = new Dictionary<string, string>();
			replacements.Add("(TM)", "™");
			replacements.Add("(R)", "®");
			replacements.Add("(C)", "©");

			int index = 0;
			foreach (KeyValuePair<string, string> kvp in replacements)
				while ((index = value.IndexOf(kvp.Key, StringComparison.OrdinalIgnoreCase)) > 0)
					value = value.Substring(0, index) + kvp.Value + value.Substring(index + kvp.Key.Length);

			return value;
		}

		#endregion
	}
}
