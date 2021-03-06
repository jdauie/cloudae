﻿using System;

namespace Jacere.Core
{
	public interface IStreamWriter : IDisposable
	{
		long Position { get; }

		void Write(byte[] array, int offset, int count);
	}
}
