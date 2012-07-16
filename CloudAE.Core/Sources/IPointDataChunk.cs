﻿using System;

namespace CloudAE.Core
{
	public interface IPointDataChunk
	{
		int Index { get; }
		byte[] Data { get; }
		unsafe byte* PointDataPtr { get; }
		unsafe byte* PointDataEndPtr { get; }
		int Length { get; }
		short PointSizeBytes { get; }
		int PointCount { get; }
	}
}
