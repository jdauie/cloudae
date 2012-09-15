using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace CloudAE.Core.Util
{
	[StructLayout(LayoutKind.Explicit)]
	public struct IMAGE_DOS_HEADER
	{
		[FieldOffset(60)]
		public int e_lfanew;
	}

	[StructLayout(LayoutKind.Explicit)]
	public struct IMAGE_NT_HEADERS32
	{
		[FieldOffset(0)]
		public uint Signature;
		[FieldOffset(4)]
		public IMAGE_FILE_HEADER FileHeader;
		[FieldOffset(24)]
		public IMAGE_OPTIONAL_HEADER32 OptionalHeader;
	}

	[StructLayout(LayoutKind.Explicit)]
	public struct IMAGE_NT_HEADERS64
	{
		[FieldOffset(0)]
		public uint Signature;
		[FieldOffset(4)]
		public IMAGE_FILE_HEADER FileHeader;
		[FieldOffset(24)]
		public IMAGE_OPTIONAL_HEADER64 OptionalHeader;
	}

	public struct IMAGE_FILE_HEADER
	{
		public ushort Machine;
		public ushort NumberOfSections;
		public ulong TimeDateStamp;
		public ulong PointerToSymbolTable;
		public ulong NumberOfSymbols;
		public ushort SizeOfOptionalHeader;
		public ushort Characteristics;
	}

	[StructLayout(LayoutKind.Explicit)]
	public struct IMAGE_OPTIONAL_HEADER32
	{
		[FieldOffset(0)]
		public ushort Magic;
		[FieldOffset(208)]
		public IMAGE_DATA_DIRECTORY DataDirectory;
	}

	[StructLayout(LayoutKind.Explicit)]
	public struct IMAGE_OPTIONAL_HEADER64
	{
		[FieldOffset(0)]
		public ushort Magic;
		[FieldOffset(224)]
		public IMAGE_DATA_DIRECTORY DataDirectory;
	}

	public struct IMAGE_DATA_DIRECTORY
	{
		public uint VirtualAddress;
		public uint Size;
	}

	public class PEHeader
	{
		private readonly bool m_isManaged;
		private readonly bool m_is64Bit;

		public static PEHeader Load(string path)
		{
			return new PEHeader(path);
		}

		public bool IsManaged
		{
			get { return m_isManaged; }
		}

		public bool Is64Bit
		{
			get { return m_is64Bit; }
		}

		private unsafe PEHeader(string path)
		{
			var data = new byte[4096];
			using (var stream = File.OpenRead(path))
				stream.Read(data, 0, data.Length);

			fixed (byte* pData = data)
			{
				IMAGE_DOS_HEADER* idh = (IMAGE_DOS_HEADER*)pData;
				IMAGE_NT_HEADERS32* inhs = (IMAGE_NT_HEADERS32*)(pData + idh->e_lfanew);

				// PE32 (0x10b) or PE32+ (0x20b)
				if (inhs->OptionalHeader.Magic == 0x20b)
				{
					m_is64Bit = true;
					if (((IMAGE_NT_HEADERS64*)inhs)->OptionalHeader.DataDirectory.Size > 0)
						m_isManaged = true;
				}
				else
				{
					if (inhs->OptionalHeader.DataDirectory.Size > 0)
						m_isManaged = true;
				}
			}
		}
	}
}
