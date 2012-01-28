using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Text;

namespace CloudAE.Core.Windows
{
	public class WinConsole : IPropertyContainer
	{
		private class WinConsoleStateHandler : ISerializeStateBinary
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
			get
			{
				return (WinAPI.GetConsoleWindow() != IntPtr.Zero);
			}
		}

		/// <summary>
		/// Gets or sets the title.
		/// </summary>
		public static string Title
		{
			//get
			//{
			//    StringBuilder sb = new StringBuilder(256);
			//    WinAPI.GetConsoleTitle(sb, sb.Capacity);
			//    return sb.ToString();
			//}
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
		public static WinConsoleColor Color
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

			Title = "Trace Console for " + Process.GetCurrentProcess().ProcessName;
			
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
				using (WinConsoleColorHandler.Handle(WinConsoleColor.White))
					Console.Write(value.Substring(0, value.Length - trimmedValue.Length + 1));
				using (WinConsoleColorHandler.Handle(WinConsoleColor.Green))
					Console.Write(trimmedValue.Substring(1, trimmedValue.Length - 2));
				using (WinConsoleColorHandler.Handle(WinConsoleColor.White))
					Console.WriteLine(value.Substring(value.Length - 1));
			}
			else if (trimmedValue.StartsWith("+ ") || trimmedValue.StartsWith("- ") || trimmedValue.StartsWith("x "))
			{
				using (WinConsoleColorHandler.Handle(WinConsoleColor.Cyan))
					Console.Write(value.Substring(0, value.Length - trimmedValue.Length + 1));
				using (WinConsoleColorHandler.Handle(WinConsoleColor.Red))
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
							using (WinConsoleColorHandler.Handle(WinConsoleColor.White))
								Console.Write('.');
						}
						using (WinConsoleColorHandler.Handle(WinConsoleColor.Magenta))
							Console.Write(segments[i]);
					}
					
					using (WinConsoleColorHandler.Handle(WinConsoleColor.Cyan))
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
}
