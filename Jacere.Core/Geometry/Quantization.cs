using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;

namespace Jacere.Core.Geometry
{
	public class Quantization : ISerializeBinary
	{
		private readonly double[] m_offset;
		private readonly double[] m_scale;

		public double[] Offset
		{
			get { return m_offset; }
		}

		public double[] Scale
		{
			get { return m_scale; }
		}

		protected Quantization(double[] scale, double[] offset)
		{
			if (offset == null || scale == null || offset.Length != scale.Length || scale.Length == 0)
				throw new ArgumentException("Invalid quantization arrays");

			m_scale = scale;
			m_offset = offset;
		}

		protected Quantization(BinaryReader reader)
		{
			var count = reader.ReadInt32();
			m_scale = reader.ReadDoubleArray(count);
			m_offset = reader.ReadDoubleArray(count);
		}

		public void Serialize(BinaryWriter writer)
		{
			writer.Write(m_scale.Length);
			writer.Write(m_scale);
			writer.Write(m_offset);
		}
	}
}
