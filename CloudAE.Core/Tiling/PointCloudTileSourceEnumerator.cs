using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace CloudAE.Core
{
	public class PointCloudTileSourceEnumerator : IEnumerator<PointCloudTileSourceEnumeratorChunk>, IEnumerable<PointCloudTileSourceEnumeratorChunk>
	{
		private readonly PointCloudTileSource m_source;
		private readonly int m_validTileCount;
		private readonly byte[] m_buffer;

		private IEnumerator<PointCloudTile> m_tileEnumerator;
		private FileStream m_stream;
		private int m_validTileIndex;

		public PointCloudTileSourceEnumerator(PointCloudTileSource source, byte[] buffer)
		{
			m_source = source;
			m_buffer = buffer;
			m_validTileCount = m_source.TileSet.ValidTileCount;

			m_stream = new FileStream(source.FilePath, FileMode.Open, FileAccess.Read, FileShare.Read, BufferManager.BUFFER_SIZE_BYTES, FileOptions.SequentialScan);

			Reset();
		}

		public PointCloudTileSourceEnumeratorChunk Current
		{
			get
			{
				return new PointCloudTileSourceEnumeratorChunk(m_tileEnumerator.Current, (float)m_validTileIndex / m_validTileCount);
			}
		}

		object System.Collections.IEnumerator.Current
		{
			get { return Current; }
		}

		public bool MoveNext()
		{
			if (m_tileEnumerator.MoveNext())
			{
				++m_validTileIndex;

				PointCloudTile tile = m_tileEnumerator.Current;
				tile.ReadTile(m_stream, m_buffer);

				return true;
			}
			return false;
		}

		public void Reset()
		{
			m_validTileIndex = 0;
			m_tileEnumerator = m_source.GetEnumerator();
		}

		public void Dispose()
		{
			m_stream.Dispose();
			m_stream = null;
		}

		public IEnumerator<PointCloudTileSourceEnumeratorChunk> GetEnumerator()
		{
			return this;
		}

		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			return this;
		}
	}
}
