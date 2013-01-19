using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Text;

namespace Jacere.Core.Windows
{
	/// <summary>
	/// Derived from WinConsole by Wesner Moise.
	/// http://www.codeproject.com/Articles/4426/Console-Enhancements
	/// </summary>
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
					WinConsole.WindowSize = new NativeMethods.Coord((short)width, (short)height);
				}
			}

			public void Serialize(BinaryWriter writer)
			{
				if (WinConsole.Initialized)
				{
					Point pos = WinConsole.WindowPosition;
					NativeMethods.Coord coord = WinConsole.WindowSize;

					writer.Write((int)pos.X);
					writer.Write((int)pos.Y);
					writer.Write((int)coord.X);
					writer.Write((int)coord.Y);
				}
			}

			#endregion
		}

		private static IntPtr c_buffer;

		private static readonly WinConsoleStateHandler c_rect;

		#region Properties

		public static Boolean Initialized
		{
			get
			{
				return (NativeMethods.GetConsoleWindow() != IntPtr.Zero);
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
				NativeMethods.SetConsoleTitle(value);
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
				return NativeMethods.GetConsoleWindow();
			}
		}

		/// <summary>
		/// Gets and sets the parent handle.
		/// </summary>
		public static IntPtr ParentHandle
		{
			get
			{
				IntPtr hwnd = NativeMethods.GetConsoleWindow();
				return NativeMethods.GetParent(hwnd);
			}
			set
			{
				IntPtr hwnd = Handle;
				if (hwnd == IntPtr.Zero)
					return;

				NativeMethods.SetParent(hwnd, value);
				int style = NativeMethods.GetWindowLong(hwnd, NativeMethods.GWL_STYLE);
				if (value == IntPtr.Zero)
					NativeMethods.SetWindowLong(hwnd, NativeMethods.GWL_STYLE, (style & ~NativeMethods.WS_CHILD) | NativeMethods.WS_OVERLAPPEDWINDOW);
				else
					NativeMethods.SetWindowLong(hwnd, NativeMethods.GWL_STYLE, (style | NativeMethods.WS_CHILD) & ~NativeMethods.WS_OVERLAPPEDWINDOW);
				NativeMethods.SetWindowPos(hwnd, IntPtr.Zero, 0, 0, 0, 0, NativeMethods.SWP_NOSIZE | NativeMethods.SWP_NOZORDER | NativeMethods.SWP_NOACTIVATE);
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
		public static NativeMethods.Coord BufferSize
		{
			set
			{
				if (!Initialized)
					return;
				IntPtr buffer = Buffer;
				if (buffer != IntPtr.Zero)
					NativeMethods.SetConsoleScreenBufferSize(buffer, value);
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
					NativeMethods.SetConsoleTextAttribute(buffer, value);
			}
		}

		/// <summary>
		/// Gets or sets the size of the console window.
		/// </summary>
		public static NativeMethods.Coord WindowSize
		{
			get
			{
				if (Initialized)
					return new NativeMethods.Coord((short)Console.WindowWidth, (short)Console.WindowHeight);
				throw new InvalidOperationException("Console not initialized.");
			}
			set
			{
				if (!Initialized)
					return;
				IntPtr buffer = Buffer;
				if (buffer != IntPtr.Zero)
				{
					var rect = new NativeMethods.SmallRect(0, 0, (short)(value.X - 1), (short)(value.Y - 1));
					NativeMethods.SetConsoleWindowInfo(buffer, true, ref rect);
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
					NativeMethods.Rect rect;
					if (hwnd != IntPtr.Zero && NativeMethods.GetWindowRect(hwnd, out rect))
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
					NativeMethods.SetWindowPos(hwnd, IntPtr.Zero, value.X, value.Y, 0, 0, NativeMethods.SWP_NOSIZE | NativeMethods.SWP_NOZORDER | NativeMethods.SWP_NOACTIVATE);
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

				if (NativeMethods.FreeConsole())
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

#warning don't create in non-interactive mode

			const short bufferWidth = 200;
			const short bufferHeight = 900;

			bool success = NativeMethods.AllocConsole();
			if (!success)
				return;

			IntPtr hwnd = NativeMethods.GetConsoleWindow();
			if (hwnd == IntPtr.Zero)
				return;

			if (!NativeMethods.IsWindowVisible(hwnd))
			{
				NativeMethods.ShowWindow(hwnd, NativeMethods.SW_SHOW);
			}

			c_buffer = NativeMethods.CreateConsoleScreenBuffer(
				NativeMethods.GENERIC_READ | NativeMethods.GENERIC_WRITE,
				NativeMethods.FILE_SHARE_READ | NativeMethods.FILE_SHARE_WRITE, 
				IntPtr.Zero, 
				NativeMethods.CONSOLE_TEXTMODE_BUFFER, 
				IntPtr.Zero
			);

			var size = new NativeMethods.Coord(bufferWidth, bufferHeight);
			BufferSize = size;

			bool result = NativeMethods.SetConsoleActiveScreenBuffer(c_buffer);

			NativeMethods.SetStdHandle(NativeMethods.STD_OUTPUT_HANDLE, c_buffer);
			NativeMethods.SetStdHandle(NativeMethods.STD_ERROR_HANDLE, c_buffer);

			Title = "Trace Console for " + Process.GetCurrentProcess().ProcessName;
			
			IntPtr hMenu = NativeMethods.GetSystemMenu( hwnd, false );
			if (hMenu != IntPtr.Zero) 
			{
				NativeMethods.DeleteMenu(hMenu, 61536 , 0);
				NativeMethods.DrawMenuBar(hwnd);
			}

			Stream s = Console.OpenStandardInput(NativeMethods.DEFAULT_CONSOLE_BUFFER_SIZE);
			StreamReader reader = null;
			if (s == Stream.Null)
				reader = StreamReader.Null;
			else
				reader = new StreamReader(s, Encoding.GetEncoding(NativeMethods.GetConsoleCP()), false, NativeMethods.DEFAULT_CONSOLE_BUFFER_SIZE);

			Console.SetIn(reader);
    
			StreamWriter writer = null;
			s = Console.OpenStandardOutput(NativeMethods.DEFAULT_CONSOLE_BUFFER_SIZE);
			if (s == Stream.Null) 
				writer = StreamWriter.Null;
			else 
			{
				writer = new StreamWriter(s, Encoding.GetEncoding(NativeMethods.GetConsoleOutputCP()), NativeMethods.DEFAULT_CONSOLE_BUFFER_SIZE);
				writer.AutoFlush = true;
			}

			Console.SetOut(writer);

			s = Console.OpenStandardError(NativeMethods.DEFAULT_CONSOLE_BUFFER_SIZE);
			if (s == Stream.Null) 
				writer = StreamWriter.Null;
			else 
			{
				writer = new StreamWriter(s, Encoding.GetEncoding(NativeMethods.GetConsoleOutputCP()), NativeMethods.DEFAULT_CONSOLE_BUFFER_SIZE);
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
				if (trimmedValue.EndsWith("]"))
				{
					using (WinConsoleColorHandler.Handle(WinConsoleColor.White))
						Console.Write(value.Substring(0, value.Length - trimmedValue.Length + 1));
					using (WinConsoleColorHandler.Handle(WinConsoleColor.Green))
						Console.Write(trimmedValue.Substring(1, trimmedValue.Length - 2));
					using (WinConsoleColorHandler.Handle(WinConsoleColor.White))
						Console.WriteLine("]");
				}
				else
				{
					int endBracketIndex = trimmedValue.IndexOf(']');
					if (endBracketIndex > -1)
					{
						using (WinConsoleColorHandler.Handle(WinConsoleColor.Green))
							Console.Write(value.Substring(0, value.Length - trimmedValue.Length + 1));
						using (WinConsoleColorHandler.Handle(WinConsoleColor.Red))
							Console.Write(trimmedValue.Substring(1, endBracketIndex - 1));
						using (WinConsoleColorHandler.Handle(WinConsoleColor.Green))
							Console.Write("]");
						Console.WriteLine(trimmedValue.Substring(endBracketIndex + 1));
					}
					else
					{
						Console.WriteLine(value);
					}
				}
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
				int separatorIndex = value.IndexOfAny(new[] { ':', '=' });
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
