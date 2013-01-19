using System;

namespace Jacere.Data.PointCloud
{
	public interface IStreamWriter : IDisposable
	{
		long Position { get; }

		void Write(byte[] array, int offset, int count);
	}
}
