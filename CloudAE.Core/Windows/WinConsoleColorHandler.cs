using System;

namespace CloudAE.Core.Windows
{
	class WinConsoleColorHandler : IDisposable
	{
		private bool m_reset;

		public static WinConsoleColorHandler Handle(WinConsoleColor color)
		{
			return new WinConsoleColorHandler(color);
		}

		private WinConsoleColorHandler(WinConsoleColor color)
		{
			if (color != WinConsoleColor.Undefined)
			{
				m_reset = true;
				WinConsole.Color = color;
			}
		}

		public void Dispose()
		{
			if (m_reset)
				WinConsole.Color = WinConsoleColor.Gray;
		}
	}
}
