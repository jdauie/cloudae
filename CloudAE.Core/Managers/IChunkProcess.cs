using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace CloudAE.Core
{
	public interface IChunkProcess
	{
		void Process(IPointDataChunk chunk);
	}

	public class ChunkProcessSet : IChunkProcess
	{
		private readonly List<IChunkProcess> m_chunkProcesses;

		public ChunkProcessSet(params IChunkProcess[] chunkProcesses)
		{
			m_chunkProcesses = new List<IChunkProcess>();
			foreach (var chunkProcess in chunkProcesses)
				if (chunkProcess != null)
					m_chunkProcesses.Add(chunkProcess);
		}

		public void Process(IPointDataChunk chunk)
		{
			foreach (var chunkProcess in m_chunkProcesses)
				chunkProcess.Process(chunk);
		}
	}
}
