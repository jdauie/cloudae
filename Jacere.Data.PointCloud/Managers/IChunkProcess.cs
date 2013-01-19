using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Jacere.Core.Geometry;

namespace Jacere.Data.PointCloud
{
	public interface IChunkProcess
	{
		IPointDataChunk Process(IPointDataChunk chunk);
	}

	/// <summary>
	/// Processing stack.
	/// </summary>
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

		public IPointDataChunk Process(IPointDataChunk chunk)
		{
			// allow filters to replace the chunk definition
			var currentChunk = chunk;
			foreach (var chunkProcess in m_chunkProcesses)
				currentChunk = chunkProcess.Process(currentChunk);

			return currentChunk;
		}
	}
}
