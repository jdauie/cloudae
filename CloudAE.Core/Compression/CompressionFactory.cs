using System;
using System.Collections.Generic;
using System.Linq;

namespace CloudAE.Core.Compression
{
	public class CompressionFactory : IFactory
	{
		private static readonly Dictionary<CompressionMethod, ICompressor> c_compressors;

		static CompressionFactory()
		{
			c_compressors = RegisterCompressors();
		}

		public static ICompressor GetCompressor(CompressionMethod method)
		{
			if (method == CompressionMethod.None)
				return null;

			if (!c_compressors.ContainsKey(method))
				throw new InvalidOperationException("Compression method unavailable.");

			return c_compressors[method];
		}

		private static Dictionary<CompressionMethod, ICompressor> RegisterCompressors()
		{
			List<ICompressor> compressors = new List<ICompressor>();
			Type baseType = typeof(ICompressor);

			Context.ProcessLoadedTypes(
				1,
				"Compressors",
				t => baseType.IsAssignableFrom(t),
				t => !t.IsAbstract,
				t => compressors.Add(Activator.CreateInstance(t) as ICompressor)
			);

			Dictionary<CompressionMethod, ICompressor> compressorLookup = new Dictionary<CompressionMethod, ICompressor>(compressors.Count);
			foreach (ICompressor compressor in compressors)
			{
				if (compressorLookup.ContainsKey(compressor.Method))
				{
					// prefer the one in core?
					// throw if neither is in core?
				}
				else
				{
					compressorLookup.Add(compressor.Method, compressor);
				}
			}

			return compressorLookup;
		}
	}
}
