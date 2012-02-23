
namespace CloudAE.Core.Compression
{
	public enum CompressionMethod : int
	{
		None = 0,
		Basic,
		QuickLZ,
		DotNetZip,
		SevenZipSharp
	};
}
