using System.IO;
using SevenZip;
using CloudAE.Core.Compression;
using CloudAE.Core;

namespace CloudAE.Compression.SevenZipSharp
{
	public class SevenZipSharp : ICompressor
	{
		public Core.Compression.CompressionMethod Method
		{
			get { return Core.Compression.CompressionMethod.SevenZipSharp; }
		}

		static SevenZipSharp()
		{
			// make a lookup for sub-files
			SevenZipCompressor.SetLibraryPath(@"..\lib\x64\7z64.dll");
		}

		public SevenZipSharp()
		{
		}

		public int Compress(PointCloudTile tile, byte[] uncompressedBuffer, int count, byte[] compressedBuffer)
		{
			SevenZipCompressor compressor = new SevenZipCompressor
			{
				CompressionMethod = SevenZip.CompressionMethod.Lzma2,
				CompressionLevel = CompressionLevel.Fast
			};

			MemoryStream uncompressedStream = new MemoryStream(uncompressedBuffer, 0, count, false);

			// custom stream is required because the position is always 32 instead of the end of the stream
			MemorableMemoryStream compressedStream = new MemorableMemoryStream(compressedBuffer);

			compressor.CompressStream(uncompressedStream, compressedStream);

			return (int)compressedStream.MaxPosition;
		}

		public int Decompress(PointCloudTile tile, byte[] compressedBuffer, int count, byte[] uncompressedBuffer)
		{
			MemoryStream uncompressedStream = new MemoryStream(uncompressedBuffer);
			MemoryStream compressedStream = new MemoryStream(compressedBuffer, 0, count, false);

			using (SevenZipExtractor extractor = new SevenZipExtractor(compressedStream))
			{
				extractor.ExtractFile(0, uncompressedStream);
			}

			return (int)uncompressedStream.Position;
		}
	}
}
