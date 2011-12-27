using System.IO;
using SevenZip;

namespace CloudAE.Core.Compression
{
	public class SevenZipSharp
	{
		static SevenZipSharp()
		{
			SevenZipCompressor.SetLibraryPath(@"..\lib\x64\7z64.dll");
		}

		public static int Compress(byte[] uncompressedBuffer, int count, byte[] compressedBuffer)
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

		public static int Decompress(byte[] compressedBuffer, int count, byte[] uncompressedBuffer)
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
