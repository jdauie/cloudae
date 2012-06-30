using System;

namespace CloudAE.Core
{
	public interface IStreamReader : IDisposable
	{
		long Position { get; }

		int Read(byte[] array, int offset, int count);
		void Seek(long position);
	}
}
