using System;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32.SafeHandles;

namespace Jacere.Core.Windows
{
	/// <summary>
	/// Win API calls.
	/// </summary>
	public static class NativeMethods
	{
		public static readonly IntPtr NULL = IntPtr.Zero;

		#region Constants

		private const String KERNEL32 = "kernel32.dll";
		private const String USER32   = "user32.dll";
		private const String ADVAPI32 = "advapi32.dll";
		private const String OLE32    = "ole32.dll";
		private const String OLEAUT32 = "oleaut32.dll";
		private const String SHFOLDER = "shfolder.dll";
		private const String SHIM     = "mscoree.dll";
		private const String CRYPT32  = "crypt32.dll";
		private const String SECUR32  = "secur32.dll";
		private const String MPR      = "mpr.dll";

		#endregion

		#region Windows

		[DllImport(KERNEL32)]
		internal static extern bool AllocConsole();

		[DllImport(KERNEL32)]
		internal static extern bool FreeConsole();

		[DllImport(KERNEL32)]
		internal static extern bool GetConsoleTitle(StringBuilder text, int size);

		[DllImport(KERNEL32)]
		internal static extern IntPtr GetConsoleWindow();

		[DllImport(KERNEL32)]
		internal static extern int SetConsoleCursorPosition(IntPtr buffer, Coord position);

		[DllImport(KERNEL32)]
		internal static extern int FillConsoleOutputCharacter(IntPtr buffer, char character, int length, Coord position, out int written);

		[DllImport(KERNEL32)]
		internal static extern bool SetConsoleTextAttribute(IntPtr hConsoleOutput, WinConsoleColor wAttributes);

		[DllImport(KERNEL32)]
		internal static extern bool SetConsoleTitle(string lpConsoleTitle);

		[DllImport(KERNEL32)]
		internal static extern bool SetConsoleActiveScreenBuffer(IntPtr handle);

		[DllImport(KERNEL32)]
		internal static extern bool WriteConsole(IntPtr handle, string s, int length, out int written, IntPtr reserved);

		[DllImport(KERNEL32)]
		internal static extern int GetConsoleCP();

		[DllImport(KERNEL32)]
		internal static extern int GetConsoleOutputCP();

		[DllImport(KERNEL32)]
		internal static extern bool GetConsoleMode(IntPtr handle, out int flags);

		[DllImport(KERNEL32)]
		internal static extern bool SetStdHandle(int handle1, IntPtr handle2);

		[DllImport(KERNEL32)]
		internal static extern IntPtr CreateConsoleScreenBuffer(int access, int share, IntPtr security, int flags, IntPtr reserved);

		[DllImport(KERNEL32)]
		internal static extern bool SetConsoleScreenBufferSize(IntPtr hConsoleOutput, Coord dwSize);

		[DllImport(KERNEL32)]
		internal static extern bool SetConsoleWindowInfo(IntPtr hConsoleOutput, bool bAbsolute, ref SmallRect lpConsoleWindow);

		[DllImport(USER32)]
		internal static extern int SetWindowLong(IntPtr hWnd, int nIndex, int newValue);

		[DllImport(USER32)]
		internal static extern int GetWindowLong(IntPtr hWnd, int nIndex);

		[DllImport(USER32)]
		internal static extern bool ShowWindow(IntPtr hwnd, int nCmdShow);

		[DllImport(USER32)]
		internal static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

		[DllImport(USER32)]
		internal static extern IntPtr FindWindowEx(IntPtr hwndParent, IntPtr hwndChildAfter, string lpClassName, string lpWindowName);

		[DllImport(USER32)]
		internal static extern IntPtr SetParent(IntPtr hwnd, IntPtr hwnd2);

		[DllImport(USER32)]
		internal static extern IntPtr GetParent(IntPtr hwnd);

		[DllImport(USER32)]
		internal static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int x, int y, int cx, int cy, int flags);

		[DllImport(USER32)]
		internal static extern bool GetWindowRect(IntPtr hWnd, out Rect lpRect);

		[DllImport(USER32)]
		internal static extern Boolean DeleteMenu(IntPtr hMenu, int uPosition, int uFlags );

		[DllImport(USER32)]
		internal static extern Boolean DrawMenuBar(IntPtr hWnd );

		[DllImport(USER32)]
		internal static extern IntPtr GetSystemMenu(IntPtr hWnd,	bool bRevert);

		[DllImport(USER32)]
		internal static extern short GetKeyState(int nVirtKey);

		[DllImport(USER32)]
		internal static extern bool BringWindowToTop(IntPtr hWnd);

		[DllImport(USER32)]
		internal static extern bool IsWindowVisible(IntPtr hWnd);

		[DllImport(USER32)]
		internal static extern bool DeleteMenu(IntPtr hMenu, int uPosition, IntPtr uFlags);

		#endregion

		#region I/O

		[DllImport(KERNEL32, SetLastError = true, CharSet = CharSet.Auto, BestFitMapping = false)]
		internal static extern SafeFileHandle CreateFile(String lpFileName,
					int dwDesiredAccess, System.IO.FileShare dwShareMode,
					SECURITY_ATTRIBUTES securityAttrs, System.IO.FileMode dwCreationDisposition,
					int dwFlagsAndAttributes, IntPtr hTemplateFile);

		[DllImport(KERNEL32, SetLastError = true)]
		unsafe internal static extern int ReadFile(SafeFileHandle handle, byte* bytes, int numBytesToRead, out int numBytesRead, IntPtr mustBeZero);

		[DllImport(KERNEL32, SetLastError = true)]
		internal static unsafe extern int WriteFile(SafeFileHandle handle, byte* bytes, int numBytesToWrite, out int numBytesWritten, IntPtr mustBeZero);

		[DllImport(KERNEL32, BestFitMapping = false, CharSet = CharSet.Auto, SetLastError = true)]
		public static extern bool GetDiskFreeSpaceEx(string drive, out long freeBytesForUser, out long totalBytes, out long freeBytes);

		[DllImport(KERNEL32, BestFitMapping = false, CharSet = CharSet.Auto, SetLastError = true)]
		internal static extern bool GetDiskFreeSpace(string path, out uint sectorsPerCluster, out uint bytesPerSector, out uint numberOfFreeClusters, out uint totalNumberOfClusters);

		[DllImport(MPR, CharSet = CharSet.Unicode, SetLastError = true)]
		internal static extern int WNetGetConnection([MarshalAs(UnmanagedType.LPTStr)] string localName, [MarshalAs(UnmanagedType.LPTStr)] StringBuilder remoteName, ref int length);

		#endregion

		#region Constants

		public const int WS_CHILD = 0x40000000;

		private const int WS_CAPTION = 0xC00000; // WS_BORDER | WS_DLGFRAME	
		private const int WS_OVERLAPPED = 0x0;
		private const int WS_SYSMENU = 0x80000;
		private const int WS_THICKFRAME = 0x40000;
		private const int WS_MINIMIZEBOX = 0x20000;
		private const int WS_MAXIMIZEBOX = 0x10000;

		public	const int WS_OVERLAPPEDWINDOW = (WS_OVERLAPPED | WS_CAPTION | WS_SYSMENU | WS_THICKFRAME | WS_MINIMIZEBOX | WS_MAXIMIZEBOX);
		
		public const int GWL_STYLE = (-16);

		public const int SW_HIDE = 0;
		public const int SW_SHOW = 5;
		public const int CONSOLE_TEXTMODE_BUFFER = 1;

		public const int DEFAULT_CONSOLE_BUFFER_SIZE = 256;

		public const int GENERIC_READ = unchecked((int) 0x80000000);
		public const int GENERIC_WRITE = 0x40000000;

		public const int FILE_SHARE_READ = 0x1;
		public const int FILE_SHARE_WRITE = 0x2;

		public const int STD_INPUT_HANDLE = -10;
		public const int STD_OUTPUT_HANDLE = -11;
		public const int STD_ERROR_HANDLE = -12;

		public const int SWP_NOSIZE = 0x1;
		private const int SWP_NOMOVE = 0x2;

		public const int SWP_NOZORDER = 0x4;
		private const int SWP_NOREDRAW = 0x8;

		public const int SWP_NOACTIVATE = 0x10;

		#endregion

		#region Structures

		[StructLayout(LayoutKind.Sequential)]
		internal class SECURITY_ATTRIBUTES
		{
			internal int nLength = 0;
			// don't remove null, or this field will disappear in bcl.small
			internal unsafe byte* pSecurityDescriptor = null;
			internal int bInheritHandle = 0;
		}

		/// <summary>
		/// Rectangle.
		/// </summary>
		[StructLayout(LayoutKind.Sequential, Pack = 1, Size = 8)]
		public struct SmallRect
		{
			public Int16 Left;
			public Int16 Top;
			public Int16 Right;
			public Int16 Bottom;

			/// <summary>
			/// Initializes a new instance of the <see cref="SmallRect"/> struct.
			/// </summary>
			/// <param name="tLeft">The left.</param>
			/// <param name="tTop">The top.</param>
			/// <param name="tRight">The right.</param>
			/// <param name="tBottom">The bottom.</param>
			public SmallRect(short tLeft, short tTop, short tRight, short tBottom)
			{
				Left = tLeft;
				Top = tTop;
				Right = tRight;
				Bottom = tBottom;
			}
		}

		/// <summary>
		/// Rectangle.
		/// </summary>
		[StructLayout(LayoutKind.Sequential)]
		public struct Rect
		{
			public Int32 Left;
			public Int32 Top;
			public Int32 Right;
			public Int32 Bottom;

			/// <summary>
			/// Initializes a new instance of the <see cref="SmallRect"/> struct.
			/// </summary>
			/// <param name="tLeft">The left.</param>
			/// <param name="tTop">The top.</param>
			/// <param name="tRight">The right.</param>
			/// <param name="tBottom">The bottom.</param>
			public Rect(int tLeft, int tTop, int tRight, int tBottom)
			{
				Left   = tLeft;
				Top    = tTop;
				Right  = tRight;
				Bottom = tBottom;
			}
		}
		
		/// <summary>
		/// Coordinate.
		/// </summary>
		public struct Coord
		{
			public short X;
			public short Y;

			/// <summary>
			/// Initializes a new instance of the <see cref="Coord"/> struct.
			/// </summary>
			/// <param name="x">The x.</param>
			/// <param name="y">The y.</param>
			public Coord(short x, short y)
			{
				X = x;
				Y = y;
			}
		}
		
		#endregion
	}
}
