using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace CloudAE.Core
{
	class FlexibleBinaryReader : BinaryReader
	{
		private readonly bool m_closeStreamWhenDisposed;

		#region Stream constructors

		//public FlexibleBinaryReader(Stream input)
		//    : base(input)
		//{
		//}

		//public FlexibleBinaryReader(Stream input, Encoding encoding)
		//    : base(input, encoding)
		//{
		//}

		//public FlexibleBinaryReader(Stream input, bool closeStreamWhenDisposed)
		//    : base(input)
		//{
		//    m_closeStreamWhenDisposed = closeStreamWhenDisposed;
		//}

		//public FlexibleBinaryReader(Stream input, Encoding encoding, bool closeStreamWhenDisposed)
		//    : base(input, encoding)
		//{
		//    m_closeStreamWhenDisposed = closeStreamWhenDisposed;
		//}

		#endregion

		#region IStreamReader constructors

		public FlexibleBinaryReader(IStreamReader input)
			: base(input as Stream)
		{
		}

		public FlexibleBinaryReader(IStreamReader input, Encoding encoding)
			: base(input as Stream, encoding)
		{
		}

		public FlexibleBinaryReader(IStreamReader input, bool closeStreamWhenDisposed)
			: base(input as Stream)
		{
			m_closeStreamWhenDisposed = closeStreamWhenDisposed;
		}

		public FlexibleBinaryReader(IStreamReader input, Encoding encoding, bool closeStreamWhenDisposed)
			: base(input as Stream, encoding)
		{
			m_closeStreamWhenDisposed = closeStreamWhenDisposed;
		}

		#endregion

		protected override void Dispose(bool disposing)
		{
			base.Dispose(m_closeStreamWhenDisposed);
		}
	}
}
