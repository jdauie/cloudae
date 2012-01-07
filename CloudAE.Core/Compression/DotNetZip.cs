//using System.IO;
//using Ionic.Zip;

//namespace CloudAE.Core.Compression
//{
//    public class DotNetZip
//    {
//        public static int Compress(byte[] uncompressedBuffer, int count, byte[] compressedBuffer)
//        {
//            MemorableMemoryStream compressedStream = new MemorableMemoryStream(compressedBuffer);

//            using (ZipOutputStream zipStream = new ZipOutputStream(compressedStream, true))
//            {
//                zipStream.CompressionMethod = Ionic.Zip.CompressionMethod.Deflate;
//                zipStream.CompressionLevel = Ionic.Zlib.CompressionLevel.BestSpeed;

//                zipStream.PutNextEntry("a");
//                zipStream.Write(uncompressedBuffer, 0, count);
//            }

//            return (int)compressedStream.MaxPosition;
//        }

//        public static int Decompress(byte[] compressedBuffer, int count, byte[] uncompressedBuffer)
//        {
//            MemoryStream compressedStream = new MemoryStream(compressedBuffer, 0, count, false);

//            int uncompressedBytes = 0;
//            using (ZipInputStream zipStream = new ZipInputStream(compressedStream, true))
//            {
//                zipStream.GetNextEntry();
//                uncompressedBytes = zipStream.Read(uncompressedBuffer, 0, uncompressedBuffer.Length);
//            }

//            return uncompressedBytes;
//        }
//    }
//}
