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
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;

namespace CloudAE.Core
{
	public static class Context
	{
		static Context()
		{
		}

		public static void SaveWindowState(ISerializeStateBinary window)
		{
			SaveState(window, "Window");
		}

		public static void LoadWindowState(ISerializeStateBinary window)
		{
			LoadState(window, "Window");
		}

		private static void SaveState(ISerializeStateBinary state, string prefix)
		{
			PropertyManager.SetProperty(CreatePathPrefix(prefix) + state.GetIdentifier(), state);
		}

		private static void LoadState(ISerializeStateBinary state, string prefix)
		{
			PropertyManager.GetProperty(CreatePathPrefix(prefix) + state.GetIdentifier(), state);
		}

		private static string CreatePathPrefix(string prefix)
		{
			if (!string.IsNullOrWhiteSpace(prefix))
				prefix = prefix.Trim() + @"\";
			else
				prefix = "";

			return prefix;
		}
	}
}
