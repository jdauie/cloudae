using System;
using System.Runtime.InteropServices;
using System.Text;

namespace CloudAE.Core.Windows
{
	/// <summary>
	/// Win API helpers for console operations.
	/// </summary>
	public class WinAPI
	{
		#region Windows API

		[DllImport("kernel32")]
		public static extern bool AllocConsole();

		[DllImport("kernel32")]
		public static extern bool FreeConsole();

		[DllImport("kernel32")]
		public static extern bool GetConsoleTitle(StringBuilder text, int size);

		[DllImport("kernel32")]
		public static extern IntPtr GetConsoleWindow();

		[DllImport("kernel32")]
		public static extern int SetConsoleCursorPosition(IntPtr buffer, Coord position);

		[DllImport("kernel32")]
		public static extern int FillConsoleOutputCharacter(IntPtr buffer, char character, int length, Coord position, out int written);

		[DllImport("kernel32")]
		public static extern bool SetConsoleTextAttribute(IntPtr hConsoleOutput, WinConsoleColor wAttributes);

		[DllImport("kernel32")]
		public static extern bool SetConsoleTitle(string lpConsoleTitle);

		[DllImport("kernel32")]
		public static extern bool SetConsoleActiveScreenBuffer(IntPtr handle);

		[DllImport("kernel32")]
		static extern bool WriteConsole(IntPtr handle, string s, int length, out int written, IntPtr reserved);

		[DllImport("kernel32")]
		public static extern int GetConsoleCP();

		[DllImport("kernel32")]
		public static extern int GetConsoleOutputCP();

		[DllImport("kernel32")]
		static extern bool GetConsoleMode(IntPtr handle, out int flags);

		[DllImport("kernel32")]
		public static extern bool SetStdHandle(int handle1, IntPtr handle2);

		[DllImport("kernel32")]
		public static extern IntPtr CreateConsoleScreenBuffer(int access, int share, IntPtr security, int flags, IntPtr reserved);

		[DllImport("kernel32")]
		public static extern bool SetConsoleScreenBufferSize(IntPtr hConsoleOutput, Coord dwSize);

		[DllImport("kernel32")]
		public static extern bool SetConsoleWindowInfo(IntPtr hConsoleOutput, bool bAbsolute, ref SmallRect lpConsoleWindow);

		[DllImport("user32")]
		public static extern int SetWindowLong(IntPtr hWnd, int nIndex, int newValue);

		[DllImport("user32")]
		public static extern int GetWindowLong(IntPtr hWnd, int nIndex);

		[DllImport("user32")]
		public static extern bool ShowWindow(IntPtr hwnd, int nCmdShow);

		[DllImport("user32")]
		public static extern IntPtr FindWindow( string lpClassName, string lpWindowName);

		[DllImport("user32")]
		public static extern IntPtr FindWindowEx(IntPtr hwndParent, IntPtr hwndChildAfter, string lpClassName, string lpWindowName);

		[DllImport("user32")]
		public static extern IntPtr SetParent(IntPtr hwnd, IntPtr hwnd2);

		[DllImport("user32")]
		public static extern IntPtr GetParent(IntPtr hwnd);

		[DllImport("user32")]
		public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int x, int y, int cx, int cy, int flags);

		[DllImport("user32")]
		public static extern bool GetWindowRect(IntPtr hWnd, out Rect lpRect);

		[DllImport("user32")]
		public static extern Boolean DeleteMenu( IntPtr hMenu, int uPosition, int uFlags );

		[DllImport("user32")]
		public static extern Boolean DrawMenuBar( IntPtr hWnd );

		[DllImport("user32")]
		public static extern IntPtr GetSystemMenu( IntPtr hWnd,	bool bRevert);

		[DllImport("user32")]
		public static extern short GetKeyState(int nVirtKey);

		[DllImport("user32")]
		public static extern bool BringWindowToTop(IntPtr hWnd);

		[DllImport("user32")]
		public static extern bool IsWindowVisible(IntPtr hWnd);

		[DllImport("user32")]
		public static extern bool DeleteMenu(IntPtr hMenu, int uPosition, IntPtr uFlags);

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
