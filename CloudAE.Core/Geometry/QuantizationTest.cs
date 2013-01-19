using System;
using System.Collections.Generic;
using System.Linq;

using Jacere.Core;
using Jacere.Core.Geometry;
using Jacere.Data.PointCloud;

namespace CloudAE.Core.Geometry
{
	public class QuantizationTest<T> : IChunkProcess
	{
		private static readonly IPropertyState<ByteSizesSmall> PROPERTY_QUANTIZATION_MEMORY_LIMIT;

		private readonly IPointCloudBinarySource m_source;
		private readonly bool m_quantized;
		private readonly int m_count;
		private readonly T[][] m_values;
		private int m_index;

		static QuantizationTest()
		{
			PROPERTY_QUANTIZATION_MEMORY_LIMIT = Context.RegisterOption(Context.OptionCategory.Tiling, "QuantizationMemoryLimit", ByteSizesSmall.MB_16);
		}

		public QuantizationTest(IPointCloudBinarySource source)
		{
			m_source = source;
			m_quantized = (m_source.Quantization != null);
			m_count = GetPrecisionTestingPointCount(source);
			m_index = 0;
			m_values = new T[3][];
			for (int i = 0; i < 3; i++)
				m_values[i] = new T[m_count];
		}

		public unsafe IPointDataChunk Process(IPointDataChunk chunk)
		{
			if (m_index + chunk.PointCount <= m_count)
			{
				byte* pb = chunk.PointDataPtr;

				if (m_quantized)
				{
					int[][] values = m_values as int[][];
					while (pb < chunk.PointDataEndPtr)
					{
						SQuantizedPoint3D* p = (SQuantizedPoint3D*)pb;
						values[0][m_index] = (*p).X;
						values[1][m_index] = (*p).Y;
						values[2][m_index] = (*p).Z;
						++m_index;
						pb += chunk.PointSizeBytes;
					}
				}
				else
				{
					double[][] values = m_values as double[][];
					while (pb < chunk.PointDataEndPtr)
					{
						Point3D* p = (Point3D*)pb;
						values[0][m_index] = (*p).X;
						values[1][m_index] = (*p).Y;
						values[2][m_index] = (*p).Z;
						++m_index;
						pb += chunk.PointSizeBytes;
					}
				}
			}

			return chunk;
		}

		public Quantization3D CreateQuantization()
		{
			if (m_quantized)
				return Quantization3D.Create(m_source.Extent, m_source.Quantization as SQuantization3D, m_values as int[][], m_index);
			else
				return Quantization3D.Create(m_source.Extent, m_values as double[][], m_index);
		}

		private static unsafe int GetPrecisionTestingPointCount(IPointCloudBinarySource source)
		{
			int maxBytesForPrecisionTest = (int)PROPERTY_QUANTIZATION_MEMORY_LIMIT.Value;
			int maxPointsForPrecisionTest = maxBytesForPrecisionTest / sizeof(SQuantizedPoint3D);
			
			// block-alignment is no longer necessary
			//int maxPointsForPrecisionTestBlockAligned = (maxPointsForPrecisionTest / source.PointsPerBuffer) * source.PointsPerBuffer;
			//int pointsToTest = (int)Math.Min(source.Count, maxPointsForPrecisionTestBlockAligned);

			int pointsToTest = (int)Math.Min(source.Count, maxPointsForPrecisionTest);
			return pointsToTest;
		}
	}
}
