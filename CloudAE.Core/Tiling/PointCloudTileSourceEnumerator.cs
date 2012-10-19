using System;
using System.Collections.Generic;
using System.Linq;

namespace CloudAE.Core
{
	public class PointCloudTileSourceEnumerator : IPointCloudChunkEnumerator<IPointDataTileChunk>
	{
		private readonly PointCloudTileSource m_source;
		private readonly BufferInstance m_buffer;
		private readonly ProgressManagerProcess m_process;

		private IEnumerator<PointCloudTile> m_tileEnumerator;
		private IStreamReader m_stream;

		private PointCloudTileSourceEnumeratorChunk m_current;

		public PointCloudTileSourceEnumerator(PointCloudTileSource source, ProgressManagerProcess process)
		{
			m_source = source;
			m_buffer = process.AcquireBuffer(source.MaxTileBufferSize, true);
			m_process = process;

			m_stream = StreamManager.OpenReadStream(source.FilePath, source.PointDataOffset);
			
			Reset();
		}

		public IPointDataTileChunk Current
		{
			get { return m_current; }
		}

		object System.Collections.IEnumerator.Current
		{
			get { return Current; }
		}

		public bool MoveNext()
		{
			// check for cancel
			if (m_current != null && m_process != null && !m_process.Update(m_current))
				return false;

			if (m_tileEnumerator.MoveNext())
			{
				PointCloudTile tile = m_tileEnumerator.Current;
				tile.ReadTile(m_stream, m_buffer.Data);

				m_current = new PointCloudTileSourceEnumeratorChunk(tile, m_buffer);

				return true;
			}
			return false;
		}

		public void Reset()
		{
			m_tileEnumerator = m_source.TileSet.GetEnumerator();
			m_current = null;
		}

		public void Dispose()
		{
			m_stream.Dispose();
			m_stream = null;
			m_current = null;
		}

		public IEnumerator<IPointDataTileChunk> GetEnumerator()
		{
			return this;
		}

		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			return this;
		}
	}
}
