using System;
using System.Collections.Generic;
using System.Linq;

namespace CloudAE.Core.Geometry
{
	public class QuantizationConverter
	{
		private readonly Grid<int> m_tileCounts;
		private readonly short m_pointSizeBytes;

		private readonly uint m_minX;
		private readonly uint m_minY;

		private readonly double m_tilesOverRangeX;
		private readonly double m_tilesOverRangeY;

		private readonly double m_scaleTranslationX;
		private readonly double m_scaleTranslationY;
		private readonly double m_scaleTranslationZ;

		private readonly double m_offsetTranslationX;
		private readonly double m_offsetTranslationY;
		private readonly double m_offsetTranslationZ;

		public QuantizationConverter(Quantization3D inputQuantization, Quantization3D outputQuantization, UQuantizedExtent2D extent, Grid<int> tileCounts, short pointSizeBytes)
		{
			m_tileCounts = tileCounts;
			m_pointSizeBytes = pointSizeBytes;

			m_minX = extent.MinX;
			m_minY = extent.MinY;

			m_tilesOverRangeX = (double)tileCounts.SizeX / extent.RangeX;
			m_tilesOverRangeY = (double)tileCounts.SizeY / extent.RangeY;

			m_scaleTranslationX = inputQuantization.ScaleFactorX / outputQuantization.ScaleFactorX;
			m_scaleTranslationY = inputQuantization.ScaleFactorY / outputQuantization.ScaleFactorY;
			m_scaleTranslationZ = inputQuantization.ScaleFactorZ / outputQuantization.ScaleFactorZ;

			m_offsetTranslationX = (inputQuantization.OffsetX - outputQuantization.OffsetX) / outputQuantization.ScaleFactorX;
			m_offsetTranslationY = (inputQuantization.OffsetY - outputQuantization.OffsetY) / outputQuantization.ScaleFactorY;
			m_offsetTranslationZ = (inputQuantization.OffsetZ - outputQuantization.OffsetZ) / outputQuantization.ScaleFactorZ;
		}

		public unsafe void Process(IPointDataChunk chunk)
		{
			byte* pb = chunk.PointDataPtr;
			while (pb < chunk.PointDataEndPtr)
			{
				SQuantizedPoint3D* p = (SQuantizedPoint3D*)pb;

				// overwrite existing values
				(*p).X = (int)((*p).X * m_scaleTranslationX + m_offsetTranslationX);
				(*p).Y = (int)((*p).Y * m_scaleTranslationY + m_offsetTranslationY);
				(*p).Z = (int)((*p).Z * m_scaleTranslationZ + m_offsetTranslationZ);

				++m_tileCounts.Data[
					(int)(((double)(*p).X - m_minX) * m_tilesOverRangeX),
					(int)(((double)(*p).Y - m_minY) * m_tilesOverRangeY)
				];

				pb += m_pointSizeBytes;
			}
		}
	}
}
