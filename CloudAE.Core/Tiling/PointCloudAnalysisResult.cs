using System;
using System.Collections.Generic;
using System.Linq;

using Jacere.Core;
using Jacere.Core.Geometry;

namespace CloudAE.Core
{
	public class PointCloudAnalysisResult
	{
		public PointCloudTileDensity Density { get; private set; }
		public Statistics Statistics { get; private set; }
		public SQuantization3D Quantization { get; private set; }
		public GridIndexSegments GridIndex { get; private set; }

		public PointCloudAnalysisResult(PointCloudTileDensity density, Statistics statistics, SQuantization3D quantization)
			: this(density, statistics, quantization, null)
		{
		}

		public PointCloudAnalysisResult(PointCloudTileDensity density, Statistics statistics, SQuantization3D quantization, GridIndexSegments gridIndex)
		{
			Density = density;
			Statistics = statistics;
			Quantization = quantization;
			GridIndex = gridIndex;
		}
	}
}
