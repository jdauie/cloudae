/*
 * Copyright (c) 2011, Joshua Morey <josh@joshmorey.com>
 * 
 * Permission to use, copy, modify, and/or distribute this software for any
 * purpose with or without fee is hereby granted, provided that the above
 * copyright notice and this permission notice appear in all copies.
 * 
 * THE SOFTWARE IS PROVIDED "AS IS" AND THE AUTHOR DISCLAIMS ALL WARRANTIES
 * WITH REGARD TO THIS SOFTWARE INCLUDING ALL IMPLIED WARRANTIES OF
 * MERCHANTABILITY AND FITNESS. IN NO EVENT SHALL THE AUTHOR BE LIABLE FOR
 * ANY SPECIAL, DIRECT, INDIRECT, OR CONSEQUENTIAL DAMAGES OR ANY DAMAGES
 * WHATSOEVER RESULTING FROM LOSS OF USE, DATA OR PROFITS, WHETHER IN AN
 * ACTION OF CONTRACT, NEGLIGENCE OR OTHER TORTIOUS ACTION, ARISING OUT OF
 * OR IN CONNECTION WITH THE USE OR PERFORMANCE OF THIS SOFTWARE.
 * 
 * http://opensource.org/licenses/ISC
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.ComponentModel;
using Jacere.Core;
using Jacere.Core.Windows;

namespace Jacere.Core
{
	public static class ContextManager
	{
		public enum OptionCategory
		{
			Tiling = 1,
			Preview2D,
			Preview3D,
			App
		}

		private const string SETTINGS_TYPE_WINDOWS = "Windows";

		private static readonly Action<string, object[]> c_writeLineAction;

		static ContextManager()
		{
			c_writeLineAction = (s, args) => Trace.WriteLine(string.Format(s, args));
		}

		public static void WriteLine()
		{
			WriteLine("");
		}

		public static void WriteLine(string value, params object[] args)
		{
			if (c_writeLineAction != null)
				c_writeLineAction(value, args);
		}

		public static void Startup()
		{
			// calling this triggers the class constructor, so 
			// this is actually happening after the constructor
		}

		public static void Shutdown()
		{
			BufferManager.Shutdown();

			WinConsole.DestroyConsole();
		}

		#region Windows

		public static void SaveWindowState(ISerializeStateBinary window)
		{
			PropertyManager.SetProperty(window, SETTINGS_TYPE_WINDOWS);
		}

		public static void LoadWindowState(ISerializeStateBinary window)
		{
			PropertyManager.GetProperty(window, SETTINGS_TYPE_WINDOWS);
		}

		#endregion
	}
}
