using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CloudAE.Core
{
	public class PointStream
	{
		private readonly PointCloudBinarySource[] m_sources;

		public PointStream(PointCloudBinarySource[] sources)
		{
			m_sources = sources;
		}
	}
}
