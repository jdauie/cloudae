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
using Microsoft.Win32;

namespace CloudAE.Core
{
	public static class Context
	{
		public enum OptionCategory
		{
			Tiling = 1
		}

		// options registration?
		// convert/remove deprecated options

		// cache options

		private const string SETTINGS_TYPE_WINDOWS = "Windows";

		static Context()
		{
		}

		public static PropertyState<T> RegisterOption<T>(OptionCategory category, string name, T defaultValue)
		{
			if (!Enum.IsDefined(typeof(OptionCategory), category))
				throw new ArgumentException("Invalid category.");

			if (string.IsNullOrWhiteSpace(name))
				throw new ArgumentException("Option registration is empty.", "name");

			if (name.IndexOfAny(new char[] { '.', '\\' }) > 0)
				throw new ArgumentException("Option registration contains invalid characters.", "name");

			string categoryName = Enum.GetName(typeof(OptionCategory), category);
			string optionName = String.Format("{0}.{1}", categoryName, name);

			TypeCode typeCode = Type.GetTypeCode(typeof(T));
			RegistryValueKind valueKind = RegistryValueKind.None;
			Func<object, object> writeConversion = null;
			Func<object, object> readConversion = null;
			switch (typeCode)
			{
				case TypeCode.Boolean:
				case TypeCode.Byte:
				case TypeCode.SByte:
				case TypeCode.Int16:
				case TypeCode.UInt16:
				case TypeCode.Int32:
				case TypeCode.UInt32:
				case TypeCode.Single:
					valueKind = RegistryValueKind.DWord;
					break;
				case TypeCode.Int64:
				case TypeCode.UInt64:
				case TypeCode.Double:
					valueKind = RegistryValueKind.QWord;
					break;
				case TypeCode.String:
					valueKind = RegistryValueKind.String;
					break;
				default:
					throw new InvalidOperationException("Unsupported property type.");
			}

			switch (typeCode)
			{
				case TypeCode.Boolean:
					writeConversion = new Func<object, object>(delegate(object value) { return (int)((bool)value ? 0 : 1); });
					readConversion = new Func<object, object>(delegate(object value) { return ((int)value == 1); });
					break;
				case TypeCode.Byte:
				case TypeCode.SByte:
				case TypeCode.Int16:
				case TypeCode.UInt16:
				case TypeCode.UInt32:
					writeConversion = new Func<object, object>(delegate(object value) { return Convert.ToInt32(value); });
					break;
				case TypeCode.UInt64:
					writeConversion = new Func<object, object>(delegate(object value) { return Convert.ToInt64(value); });
					break;
				case TypeCode.Single:
					readConversion = new Func<object, object>(delegate(object value) { return BitConverter.ToSingle(BitConverter.GetBytes((int)value), 0); });
					break;
				case TypeCode.Double:
					readConversion = new Func<object, object>(delegate(object value) { return BitConverter.ToDouble(BitConverter.GetBytes((long)value), 0); });
					break;
			}

			PropertyState<T> state = new PropertyState<T>(optionName, valueKind, defaultValue, writeConversion, readConversion);

			return state;
		}

		public static void GetOptionValue(OptionCategory category, string name)
		{

		}

		

		#region Windows

		public static void SaveWindowState(ISerializeStateBinary window)
		{
			SaveState(window, SETTINGS_TYPE_WINDOWS);
		}

		public static void LoadWindowState(ISerializeStateBinary window)
		{
			LoadState(window, SETTINGS_TYPE_WINDOWS);
		}

		#endregion

		#region Property Helpers

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

		#endregion
	}
}
