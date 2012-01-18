
namespace CloudAE.Core.Compression
{
	public interface ICompressor
	{
		CompressionMethod Method
		{
			get;
		}

		int Compress(PointCloudTile tile, byte[] uncompressedBuffer, int count, byte[] compressedBuffer);
		int Decompress(PointCloudTile tile, byte[] compressedBuffer, int count, byte[] uncompressedBuffer);
	}
}
