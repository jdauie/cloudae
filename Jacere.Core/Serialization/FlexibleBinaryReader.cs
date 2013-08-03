using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Jacere.Core
{
#warning rename to NonClosingBinaryReader? or something similar
	public class FlexibleBinaryReader : BinaryReader
	{
		private readonly bool m_closeStreamWhenDisposed;

		public FlexibleBinaryReader(Stream input)
			: base(input)
		{
		}

		public FlexibleBinaryReader(Stream input, Encoding encoding)
			: base(input, encoding)
		{
		}

		public FlexibleBinaryReader(Stream input, bool closeStreamWhenDisposed)
			: base(input)
		{
			m_closeStreamWhenDisposed = closeStreamWhenDisposed;
		}

		public FlexibleBinaryReader(Stream input, Encoding encoding, bool closeStreamWhenDisposed)
			: base(input, encoding)
		{
			m_closeStreamWhenDisposed = closeStreamWhenDisposed;
		}

		protected override void Dispose(bool disposing)
		{
			base.Dispose(m_closeStreamWhenDisposed);
		}
	}
}
