using System;
using System.Collections.Generic;
using System.Linq;

using CloudAE.Core.Geometry;

namespace CloudAE.Core
{
	public class PointCloudAnalysisResult
	{
		public PointCloudTileDensity Density { get; private set; }
		public Statistics Statistics { get; private set; }
		public Quantization3D Quantization { get; private set; }
		public GridIndexSegments GridIndex { get; private set; }

		public PointCloudAnalysisResult(PointCloudTileDensity density, Statistics statistics, Quantization3D quantization)
			: this(density, statistics, quantization, null)
		{
		}

		public PointCloudAnalysisResult(PointCloudTileDensity density, Statistics statistics, Quantization3D quantization, GridIndexSegments gridIndex)
		{
			Density = density;
			Statistics = statistics;
			Quantization = quantization;
			GridIndex = gridIndex;
		}
	}
}
