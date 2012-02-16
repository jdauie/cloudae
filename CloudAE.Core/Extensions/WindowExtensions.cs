using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.IO;

namespace CloudAE.Core
{
	public static class WindowExtensions
	{
		public static void SerializeState(this Window target, BinaryWriter writer)
		{
			writer.Write((int)target.Left);
			writer.Write((int)target.Top);
			writer.Write((int)target.Width);
			writer.Write((int)target.Height);

			writer.Write(target.WindowState == System.Windows.WindowState.Maximized);
		}

		public static bool DeserializeState(this Window target, BinaryReader reader)
		{
			if ((reader.BaseStream.Length - reader.BaseStream.Position) < (4 * sizeof(int) + sizeof(bool)))
				return false;

			target.Left   = reader.ReadInt32();
			target.Top    = reader.ReadInt32();
			target.Width  = reader.ReadInt32();
			target.Height = reader.ReadInt32();

			if (reader.ReadBoolean())
				target.WindowState = System.Windows.WindowState.Maximized;

			return true;
		}
	}
}
