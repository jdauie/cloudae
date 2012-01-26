using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.IO;
using Microsoft.Win32;
using System.Reflection;
using System.Diagnostics;
using System.IO.Compression;

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
			Registry    = 1 << 4,
			AllDrives   = 1 << 5,
			FullDebug   = 1 << 6
		}

		private const string DATETIME_FORMAT = "yyyy-MM-dd HH:mm:ss";

		private static readonly Process m_process;

		private static Dictionary<string, bool> m_initialSystemInfo;

		static SystemInfo()
		{
			m_process = Process.GetCurrentProcess();

			m_initialSystemInfo = null;
		}

		public static string GetSystemInfo()
		{
			DebugInfo flags =
				DebugInfo.Environment |
				DebugInfo.Memory |
				DebugInfo.Context;

			if (m_process != null) flags |= DebugInfo.Process;

			if (m_initialSystemInfo == null)
				flags |= DebugInfo.FullDebug;

			//flags |= DebugInfo.AllDrives;

			string systemInfo = string.Empty;
			try
			{
				systemInfo = GetSystemInfo(flags);
			}
			catch
			{
				systemInfo = "Logging error: Failed to retrieve system info.";
			}
			return systemInfo;
		}

		#region System Info Methods

		private static string GetSystemInfo(DebugInfo debugFlags)
		{
			List<string> templateLines = GetSystemInfoTemplate(debugFlags);
			List<string> outputLines = new List<string>();

			for (int i = 0; i < templateLines.Count; i++)
			{
				List<string> newLines = GenerateLines(templateLines[i]);
				outputLines.AddRange(newLines);
			}

			if (m_initialSystemInfo == null)
			{
				m_initialSystemInfo = new Dictionary<string, bool>();
				foreach (string line in outputLines)
				{
					if (!m_initialSystemInfo.ContainsKey(line))
						m_initialSystemInfo.Add(line, !LineIsHeader(line));
				}
			}
			else
			{
				// remove lines that haven't changed (except section headers)
				for (int i = 0; i < outputLines.Count; i++)
				{
					string line = outputLines[i];
					if (m_initialSystemInfo.ContainsKey(line) && m_initialSystemInfo[line])
					{
						outputLines.RemoveAt(i);
						--i;
					}
				}
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

			return string.Join(Environment.NewLine, outputLines.ToArray());
		}

		private static List<string> GetVariableUsages(string templateLine)
		{
			Regex matchVariables = new Regex(@"\$\w+", RegexOptions.Compiled);
			MatchCollection matches = matchVariables.Matches(templateLine);

			List<string> variables = new List<string>();
			foreach (Match match in matches)
			{
				string variableName = match.Value.Substring(1);

				if (!variables.Contains(variableName))
					variables.Add(variableName);
			}

			return variables;
		}

		private static List<string> GenerateLines(string sourceLine)
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
					// the GetDrives() call is *slow* (potentially a few seconds)
					DriveInfo[] allDrives = DriveInfo.GetDrives();
					foreach (DriveInfo d in allDrives)
					{
						string newLine = sourceLine;
						newLine = ReplaceVariable(newLine, "DriveLetter", GetDriveName(d));
						newLine = ReplaceVariable(newLine, "DriveDetails", GetDriveDetails(d));
						newLines.Add(newLine);
					}
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

		//private static string GetInterestingRegistryInfo()
		//{
		//    List<string> info = new List<string>();

		//    using (RegistryKey regCU = Registry.CurrentUser)
		//    {
		//        info.Add(GetInterestingRegistryInfo(regCU));
		//    }

		//    using (RegistryKey regLM = Registry.LocalMachine)
		//    {
		//        info.Add(GetInterestingRegistryInfo(regLM));
		//    }

		//    string infoString = string.Join("\n", info.ToArray());
		//    string base64 = null;
		//    byte[] byteArray = Encoding.UTF8.GetBytes(infoString);
		//    using (MemoryStream ms = new MemoryStream(byteArray))
		//    {
		//        MemoryStream cms = new MemoryStream();
		//        using (Stream cs = GetCompressedStream(cms))
		//        {
		//            CopyStream(ms, cs);
		//        }
		//        base64 = Convert.ToBase64String(cms.ToArray());
		//    }
		//    return base64;
		//}

		//private static string GetInterestingRegistryInfo(RegistryKey registryHive)
		//{
		//    List<string> registryValues = new List<string>();
		//    using (RegistryKey regOWG = registryHive.OpenSubKey(FileSystem.OWG_REGISTRY_HIVE))
		//    {
		//        if (regOWG != null)
		//        {
		//            foreach (string valueName in regOWG.GetValueNames())
		//            {
		//                object data = regOWG.GetValue(valueName);
		//                if (data != null)
		//                {
		//                    string value = String.Format("[{0}={1}]", valueName, data.ToString());
		//                    registryValues.Add(value);
		//                }
		//            }
		//        }
		//    }

		//    string values = string.Join("\n", registryValues.ToArray());
		//    return string.Format("[{0}]\n{1}", registryHive.Name, values);
		//}

		private static string GetVariableValue(string variableName)
		{
			try
			{
				switch (variableName)
				{
					case "User":                  return Environment.UserName;
					case "Domain":                return Environment.UserDomainName;
					case "Machine":               return Environment.MachineName;
					case "OS":                    return Environment.OSVersion.ToString();
					case "CLR":                   return Environment.Version.ToString();
					case "Processors":            return Environment.ProcessorCount.ToString();
					case "TempDriveLetter":       return GetDriveName(Cache.APP_CACHE_DRIVE);
					case "TempDriveDetails":      return GetDriveDetails(Cache.APP_CACHE_DRIVE);

					//case "Platform":              return GetProperty(m_context, "GISPlatformInfo").ToString();
					//case "Framework":             return GetProperty(m_context, "FrameworkVersion").ToString();
					//case "Install":               return GetProperty(m_context, "InstallPath").ToString();
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

					//case "Registry":              return GetInterestingRegistryInfo();

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
				templateLines.Add(@"	User:        $Domain\$User");
				templateLines.Add(@"	Machine:     $Machine");
				templateLines.Add(@"	OS Version:  $OS");
				templateLines.Add(@"	CLR Version: $CLR");
				templateLines.Add(@"	Processors:  $Processors");
				if ((infoFlags & DebugInfo.AllDrives) > 0)
					templateLines.Add(@"	Drive $DriveLetter:     $DriveDetails");
				else
					templateLines.Add(@"	Drive $TempDriveLetter:     $TempDriveDetails");
			}
			if ((infoFlags & DebugInfo.Context) > 0)
			{
				templateLines.Add(@"[Context]");
				templateLines.Add(@"	Platform:    $Platform");
				templateLines.Add(@"	Framework:   $Framework");
				templateLines.Add(@"	Install:     $Install");
				templateLines.Add(@"	Temp:        $Temp");
				templateLines.Add(@"	Temp Used:   $TempUsed");
			}
			if ((infoFlags & DebugInfo.Process) > 0)
			{
				templateLines.Add(@"[Process]");
				templateLines.Add(@"	Name:        $ProcName");
				templateLines.Add(@"	ID:          $ProcId");
				templateLines.Add(@"	Start Time:  $ProcStart");
				templateLines.Add(@"	Handles:     $ProcHandles");
				templateLines.Add(@"	Total CPU:   $ProcTotalCpu");
				templateLines.Add(@"	User CPU:    $ProcUserCpu");
			}
			if ((infoFlags & DebugInfo.Memory) > 0)
			{
				templateLines.Add(@"[Memory]");
				templateLines.Add(@"	Working Set: $ProcWorkingSet ($PeakProcWorkingSet)");
				templateLines.Add(@"	Paged:       $ProcPagedMemory ($PeakProcPagedMemory)");
				templateLines.Add(@"	Virtual:     $ProcVirtualMemory ($PeakProcVirtualMemory)");
			}
			if ((infoFlags & DebugInfo.Registry) > 0)
			{
				if ((infoFlags & DebugInfo.FullDebug) > 0)
				{
					templateLines.Add(@"[Registry=$Registry]");
				}
			}

			return templateLines;
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
				driveDetails = d.DriveType.ToString();
				if (d.IsReady)
				{
					driveDetails += String.Format(" {0}, {1} total, {2} free", d.DriveFormat, d.TotalSize.ToSize(), d.TotalFreeSpace.ToSize());
					if (d.AvailableFreeSpace < d.TotalFreeSpace)
						driveDetails += String.Format(", {0} available", d.AvailableFreeSpace.ToSize());
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

		#endregion
	}
}
