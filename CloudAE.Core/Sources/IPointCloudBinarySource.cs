using System;
using CloudAE.Core.Geometry;

namespace CloudAE.Core
{
	public interface IPointCloudBinarySource : IPointCloudBinarySourceEnumerable
	{
		Extent3D Extent { get; }
		Quantization3D Quantization { get; }
	}
}
