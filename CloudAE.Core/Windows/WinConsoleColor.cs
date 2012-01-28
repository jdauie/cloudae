using System;

namespace CloudAE.Core.Windows
{
	/// <summary>
	/// Console foreground colors.
	/// </summary>
	[Flags]
	public enum WinConsoleColor : short
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
}
