using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using Jacere.Core;
using Jacere.Interop.LASzip;

namespace Jacere.Data.PointCloud
{
	/// <summary>
	/// Wrapper providing access to an LAZ file as if it is logically an LAS.
	/// In the future, the LAZInterop will need a custom streambuf so it can
	/// implement unbuffered IO.
	/// </summary>
	public class LAZStreamReader : IStreamReader
	{
		private readonly string m_path;
		private readonly LASHeader m_header;
		private readonly LASVLR m_lazEncodedVLR;
		private readonly LAZInterop m_laz;

		public string Path
		{
			get { return m_path; }
		}

		public long Position
		{
			get
			{
				return m_laz.GetPosition();
			}
		}

		public LAZStreamReader(string path, LASHeader header, LASVLR lazEncodedVLR)
		{
			m_path = path;
			m_header = header;
			m_lazEncodedVLR = lazEncodedVLR;
			m_laz = new LAZInterop(m_path, m_header.OffsetToPointData, m_lazEncodedVLR.Data);
		}

		public int Read(byte[] array, int offset, int count)
		{
			int bytesRead = m_laz.Read(array, offset, count);

			return bytesRead;
		}

		public void Seek(long position)
		{
			if (position < m_header.OffsetToPointData)
				throw new Exception("This needs more work");

			// I don't know what to do about evlrs at the end, since I don't know how long the compressed data is
			// (it's probably in the vlr)

			m_laz.Seek(position);
		}

		public void Dispose()
		{
			m_laz.Dispose();
		}
	}
}
