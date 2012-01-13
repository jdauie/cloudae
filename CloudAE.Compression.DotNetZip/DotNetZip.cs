using System.IO;
using Ionic.Zip;
using CloudAE.Core.Compression;
using CloudAE.Core;

namespace CloudAE.Compression.DotNetZip
{
	public class DotNetZip : ICompressor
	{
		public Core.Compression.CompressionMethod Method
		{
			get { return Core.Compression.CompressionMethod.DotNetZip; }
		}

		public DotNetZip()
		{
		}

		public int Compress(PointCloudTile tile, byte[] uncompressedBuffer, int count, byte[] compressedBuffer)
		{
			MemorableMemoryStream compressedStream = new MemorableMemoryStream(compressedBuffer);

			using (ZipOutputStream zipStream = new ZipOutputStream(compressedStream, true))
			{
				zipStream.CompressionMethod = Ionic.Zip.CompressionMethod.Deflate;
				zipStream.CompressionLevel = Ionic.Zlib.CompressionLevel.BestSpeed;

				zipStream.PutNextEntry("a");
				zipStream.Write(uncompressedBuffer, 0, count);
			}

			return (int)compressedStream.MaxPosition;
		}

		public int Decompress(PointCloudTile tile, byte[] compressedBuffer, int count, byte[] uncompressedBuffer)
		{
			MemoryStream compressedStream = new MemoryStream(compressedBuffer, 0, count, false);

			int uncompressedBytes = 0;
			using (ZipInputStream zipStream = new ZipInputStream(compressedStream, true))
			{
				zipStream.GetNextEntry();
				uncompressedBytes = zipStream.Read(uncompressedBuffer, 0, uncompressedBuffer.Length);
			}

			return uncompressedBytes;
		}
	}
}
