using System;

namespace Jacere.Core
{
	public interface IStreamReader : IDisposable
	{
		string Path { get; }
		long Position { get; }

		int Read(byte[] array, int offset, int count);
		void Seek(long position);
	}
}
