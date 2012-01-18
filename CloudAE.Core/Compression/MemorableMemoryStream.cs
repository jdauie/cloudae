using System.IO;

namespace CloudAE.Core.Compression
{
	public class MemorableMemoryStream : MemoryStream
	{
		private long m_maxPosition;

		public long MaxPosition
		{
			get { return m_maxPosition; }
		}

		public MemorableMemoryStream(byte[] buffer)
			: base(buffer)
		{
			m_maxPosition = 0;
		}

		public override void Write(byte[] buffer, int offset, int count)
		{
			base.Write(buffer, offset, count);

			if(Position > m_maxPosition)
				m_maxPosition = Position;
		}
	}
}
