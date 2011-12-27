using System;
using System.Linq;
using System.Collections.Generic;

namespace CloudAE.Core
{
	public static class NumericExtensions
	{
		public static Statistics ComputeStatistics(this IEnumerable<float> values, float nodata)
		{
			return new Statistics(values, nodata);
		}
	}
}
