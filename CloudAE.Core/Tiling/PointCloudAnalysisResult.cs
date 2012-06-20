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

		public PointCloudAnalysisResult(PointCloudTileDensity density, Statistics statistics, Quantization3D quantization)
		{
			Density = density;
			Statistics = statistics;
			Quantization = quantization;
		}
	}
}
