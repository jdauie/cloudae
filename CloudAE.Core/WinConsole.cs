using System;
using System.Drawing;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using System.IO;

namespace CloudAE.Core
{
	/// <summary>
	/// Console foreground colors.
	/// </summary>
	[Flags]
	public enum ForeGroundColour : short
	{
		Violet        = 0x0005,
		Intensified   = 0x0008,
		Normal        = White,

		BlackBG       = 0x0000,
		BlueBG        = 0x0010,
		GreenBG       = 0x0020,
		CyanBG        = 0x0030,
		RedBG         = 0x0040,
		VioletBG      = 0x0050,
		YellowBG      = 0x0060,
		WhiteBG       = 0x0070,

		Black         = 0x0000,
		Blue          = 0x0001,
		Green         = 0x0002,
		Cyan          = 0x0003,
		Red           = 0x0004,
		Magenta       = 0x0005,
		Yellow        = 0x0006,
		Gray          = 0x0007,
		White         = 0x0008,

		IntensifiedBG = 0x0080,
		Underline     = 0x4000,
		Undefined     = 0x7FFF
	}

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
		public static extern bool SetConsoleTextAttribute(IntPtr hConsoleOutput, ForeGroundColour wAttributes);

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

	public class WinConsole : IPropertyContainer
	{
		class WinConsoleStateHandler : ISerializeStateBinary
		{
			#region ISerializeStateBinary Members

			public string GetIdentifier()
			{
				return "WinConsole";
			}

			public void Deserialize(BinaryReader reader)
			{
				if (WinConsole.Initialized)
				{
					int left   = reader.ReadInt32();
					int top    = reader.ReadInt32();
					int width  = reader.ReadInt32();
					int height = reader.ReadInt32();

					WinConsole.WindowPosition = new Point(left, top);
					WinConsole.WindowSize = new WinAPI.Coord((short)width, (short)height);
				}
			}

			public void Serialize(BinaryWriter writer)
			{
				if (WinConsole.Initialized)
				{
					Point pos = WinConsole.WindowPosition;
					WinAPI.Coord coord = WinConsole.WindowSize;

					writer.Write((int)pos.X);
					writer.Write((int)pos.Y);
					writer.Write((int)coord.X);
					writer.Write((int)coord.Y);
				}
			}

			#endregion
		}

		private static IntPtr c_buffer;

		private static WinConsoleStateHandler c_rect;

		#region Properties

		public static Boolean Initialized
		{
			get { return (WinAPI.GetConsoleWindow() != IntPtr.Zero); }
		}

		/// <summary>
		/// Gets or sets the title.
		/// </summary>
		public static string Title
		{
			get
			{
				StringBuilder sb = new StringBuilder(256);
				WinAPI.GetConsoleTitle(sb, sb.Capacity);
				return sb.ToString();
			}
			set
			{
				WinAPI.SetConsoleTitle(value);
			}
		}

		/// <summary>
		/// Get the handle of the console window.
		/// </summary>
		public static IntPtr Handle
		{
			get
			{
				Initialize();
				return WinAPI.GetConsoleWindow();
			}
		}

		/// <summary>
		/// Gets and sets the parent handle.
		/// </summary>
		public static IntPtr ParentHandle
		{
			get
			{
				IntPtr hwnd = WinAPI.GetConsoleWindow();
				return WinAPI.GetParent(hwnd);
			}
			set
			{
				IntPtr hwnd = Handle;
				if (hwnd == IntPtr.Zero)
					return;

				WinAPI.SetParent(hwnd, value);
				int style = WinAPI.GetWindowLong(hwnd, WinAPI.GWL_STYLE);
				if (value == IntPtr.Zero)
					WinAPI.SetWindowLong(hwnd, WinAPI.GWL_STYLE, (style & ~WinAPI.WS_CHILD) | WinAPI.WS_OVERLAPPEDWINDOW);
				else
					WinAPI.SetWindowLong(hwnd, WinAPI.GWL_STYLE, (style | WinAPI.WS_CHILD) & ~WinAPI.WS_OVERLAPPEDWINDOW);
				WinAPI.SetWindowPos(hwnd, IntPtr.Zero, 0, 0, 0, 0, WinAPI.SWP_NOSIZE | WinAPI.SWP_NOZORDER | WinAPI.SWP_NOACTIVATE);
			}
		}

		/// <summary>
		/// Gets the current buffer handle.
		/// </summary>
		public static IntPtr Buffer
		{
			get
			{
				if (!Initialized) Initialize();
				return c_buffer;
			}
		}

		/// <summary>
		/// Sets the current buffer size.
		/// </summary>
		public static WinAPI.Coord BufferSize
		{
			set
			{
				if (!Initialized)
					return;
				IntPtr buffer = Buffer;
				if (buffer != IntPtr.Zero)
					WinAPI.SetConsoleScreenBufferSize(buffer, value);
			}
		}

		/// <summary>
		/// Sets the current text color.
		/// </summary>
		public static ForeGroundColour Color
		{
			set
			{
				if (!Initialized)
					return;
				IntPtr buffer = Buffer;
				if (buffer != IntPtr.Zero)
					WinAPI.SetConsoleTextAttribute(buffer, value);
			}
		}

		/// <summary>
		/// Gets or sets the size of the console window.
		/// </summary>
		public static WinAPI.Coord WindowSize
		{
			get
			{
				if (Initialized)
					return new WinAPI.Coord((short)Console.WindowWidth, (short)Console.WindowHeight);
				throw new InvalidOperationException("Console not initialized.");
			}
			set
			{
				if (!Initialized)
					return;
				IntPtr buffer = Buffer;
				if (buffer != IntPtr.Zero)
				{
					WinAPI.SmallRect rect = new WinAPI.SmallRect(0, 0, (short)(value.X - 1), (short)(value.Y - 1));
					WinAPI.SetConsoleWindowInfo(buffer, true, ref rect);
				}
			}
		}

		/// <summary>
		/// Gets or sets the position of the console window.
		/// </summary>
		public static Point WindowPosition
		{
			get
			{
				if (Initialized)
				{
					IntPtr hwnd = Handle;
					WinAPI.Rect rect;
					if (hwnd != IntPtr.Zero && WinAPI.GetWindowRect(hwnd, out rect))
						return new Point(rect.Left, rect.Top);
				}
				throw new InvalidOperationException("Console not initialized.");
			}
			set
			{
				if (!Initialized)
					return;
				IntPtr buffer = Buffer;
				IntPtr hwnd = Handle;
				if (buffer != IntPtr.Zero && hwnd != IntPtr.Zero)
					WinAPI.SetWindowPos(hwnd, IntPtr.Zero, value.X, value.Y, 0, 0, WinAPI.SWP_NOSIZE | WinAPI.SWP_NOZORDER | WinAPI.SWP_NOACTIVATE);
			}
		}

		#endregion

		static WinConsole()
		{
			c_rect = new WinConsoleStateHandler();
		}

		private WinConsole()
		{
			Initialize();
		}

		/// <summary>
		/// Closes the debug console.
		/// </summary>
		public static bool DestroyConsole()
		{
			bool result = false;

			if (Initialized)
			{
				Context.SaveWindowState(c_rect);

				if (WinAPI.FreeConsole())
				{
					c_buffer = IntPtr.Zero;
					result = true;
				}
			}

			return result;
		}

		/// <summary>
		/// Initializes the console.
		/// </summary>
		public static void Initialize()
		{
			if (Initialized)
				return;

			//if (!SystemUtilities.UserInteractiveMode)
			//    return;

			const short bufferWidth = 100;
			const short bufferHeight = 900;

			bool success = WinAPI.AllocConsole();
			if (!success)
				return;

			IntPtr hwnd = WinAPI.GetConsoleWindow();
			if (hwnd == IntPtr.Zero)
				return;

			if (!WinAPI.IsWindowVisible(hwnd))
			{
				WinAPI.ShowWindow(hwnd, WinAPI.SW_SHOW);
			}

			c_buffer = WinAPI.CreateConsoleScreenBuffer(
				WinAPI.GENERIC_READ | WinAPI.GENERIC_WRITE,
				WinAPI.FILE_SHARE_READ | WinAPI.FILE_SHARE_WRITE, 
				IntPtr.Zero, 
				WinAPI.CONSOLE_TEXTMODE_BUFFER, 
				IntPtr.Zero
			);

			WinAPI.Coord size = new WinAPI.Coord(bufferHeight, bufferWidth);
			BufferSize = size;

			bool result = WinAPI.SetConsoleActiveScreenBuffer(c_buffer);

			WinAPI.SetStdHandle(WinAPI.STD_OUTPUT_HANDLE, c_buffer);
			WinAPI.SetStdHandle(WinAPI.STD_ERROR_HANDLE, c_buffer);

			Title = "Trace Console for " + Title;
			
			IntPtr hMenu = WinAPI.GetSystemMenu( hwnd, false );
			if (hMenu != IntPtr.Zero) 
			{
				WinAPI.DeleteMenu(hMenu, 61536 , 0);
				WinAPI.DrawMenuBar(hwnd);
			}

			Stream s = Console.OpenStandardInput(WinAPI.DEFAULT_CONSOLE_BUFFER_SIZE);
			StreamReader reader = null;
			if (s == Stream.Null)
				reader = StreamReader.Null;
			else
				reader = new StreamReader(s, Encoding.GetEncoding(WinAPI.GetConsoleCP()), false, WinAPI.DEFAULT_CONSOLE_BUFFER_SIZE);

			Console.SetIn(reader);
    
			StreamWriter writer = null;
			s = Console.OpenStandardOutput(WinAPI.DEFAULT_CONSOLE_BUFFER_SIZE);
			if (s == Stream.Null) 
				writer = StreamWriter.Null;
			else 
			{
				writer = new StreamWriter(s, Encoding.GetEncoding(WinAPI.GetConsoleOutputCP()), WinAPI.DEFAULT_CONSOLE_BUFFER_SIZE);
				writer.AutoFlush = true;
			}

			Console.SetOut(writer);

			s = Console.OpenStandardError(WinAPI.DEFAULT_CONSOLE_BUFFER_SIZE);
			if (s == Stream.Null) 
				writer = StreamWriter.Null;
			else 
			{
				writer = new StreamWriter(s, Encoding.GetEncoding(WinAPI.GetConsoleOutputCP()), WinAPI.DEFAULT_CONSOLE_BUFFER_SIZE);
				writer.AutoFlush = true;
			}
			
			Console.SetError(writer);

			Context.LoadWindowState(c_rect);
		}

		public static void WriteLine(string format, params object[] args)
		{
			string value = string.Format(format, args);
			string trimmedValue = value.TrimStart();

			if (trimmedValue.StartsWith("["))
			{
				using (WinConsoleColorHandler.Handle(ForeGroundColour.White))
					Console.Write(value.Substring(0, value.Length - trimmedValue.Length + 1));
				using (WinConsoleColorHandler.Handle(ForeGroundColour.Green))
					Console.Write(trimmedValue.Substring(1, trimmedValue.Length - 2));
				using (WinConsoleColorHandler.Handle(ForeGroundColour.White))
					Console.WriteLine(value.Substring(value.Length - 1));
			}
			else if (trimmedValue.StartsWith("+ ") || trimmedValue.StartsWith("- ") || trimmedValue.StartsWith("x "))
			{
				using (WinConsoleColorHandler.Handle(ForeGroundColour.Cyan))
					Console.Write(value.Substring(0, value.Length - trimmedValue.Length + 1));
				using (WinConsoleColorHandler.Handle(ForeGroundColour.Red))
					Console.WriteLine(trimmedValue.Substring(1));
			}
			else
			{
				int separatorIndex = value.IndexOfAny(new char[] { ':', '=' });
				if (separatorIndex > 0 && separatorIndex < value.Length - 1 && value[separatorIndex + 1] == ' ')
				{
					string[] segments = value.Substring(0, separatorIndex).Split('.');
					for (int i = 0; i < segments.Length; i++)
					{
						if (i > 0)
						{
							using (WinConsoleColorHandler.Handle(ForeGroundColour.White))
								Console.Write('.');
						}
						using (WinConsoleColorHandler.Handle(ForeGroundColour.Magenta))
							Console.Write(segments[i]);
					}
					
					using (WinConsoleColorHandler.Handle(ForeGroundColour.Cyan))
						Console.Write(value.Substring(separatorIndex, 1));
					Console.WriteLine(value.Substring(separatorIndex + 1));
				}
				else
				{
					Console.WriteLine(value);
				}
			}

			
		}
	}

	class WinConsoleColorHandler : IDisposable
	{
		private bool m_reset;

		public static WinConsoleColorHandler Handle(ForeGroundColour color)
		{
			return new WinConsoleColorHandler(color);
		}

		private WinConsoleColorHandler(ForeGroundColour color)
		{
			if (color != ForeGroundColour.Undefined)
			{
				m_reset = true;
				WinConsole.Color = color;
			}
		}

		public void Dispose()
		{
			if (m_reset)
				WinConsole.Color = ForeGroundColour.Gray;
		}
	}
}
