using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Diagnostics;
using CloudAE.Core.Geometry;
using CloudAE.Core.Compression;

namespace CloudAE.Core
{
	class LASFile : FileHandlerBase
	{
		private const bool TRUST_HEADER_EXTENT = false;

		private const int minimumSizeOfHeader = 221;

		private const int pSignature = 0;
		private const int pPointOffset = 96;
		private const int pPointDataRecordLength = 105;
		private const int pNumPointRecords = 107;
		private const int pQuantizedScaleFactors = 131;


		private string m_path;

		private readonly int m_pointCount;

		public readonly int PointOffset;
		public readonly short PointDataRecordLength;

		public readonly SQuantization3D Quantization;

		// not to be trusted
		private Extent3D m_headerExtent;

		public override string[] SupportedExtensions
		{
			get { return new string[] { "las" }; }
		}

		public unsafe LASFile(string path)
			: base(path)
		{
			m_path = path;

			using (FileStream inputStream = new FileStream(m_path, FileMode.Open, FileAccess.Read, FileShare.None, minimumSizeOfHeader))
			{
				long inputLength = inputStream.Length;

				if (inputLength < minimumSizeOfHeader)
					throw new Exception("Invalid format: header too short");

				byte[] inputBuffer = BufferManager.AcquireBuffer();

				// read header
				int bytesRead = inputStream.Read(inputBuffer, 0, BufferManager.BUFFER_SIZE_BYTES);

				fixed (byte* inputBufferPtr = inputBuffer)
				{
					// check signature

					int* pointOffsetPtr = (int*)(inputBufferPtr + pPointOffset);
					PointOffset = pointOffsetPtr[0];

					short* pointSizePtr = (short*)(inputBufferPtr + pPointDataRecordLength);
					PointDataRecordLength = pointSizePtr[0];

					int* pointCountPtr = (int*)(inputBufferPtr + pNumPointRecords);
					m_pointCount = pointCountPtr[0];

					double* quantizationPtr = (double*)(inputBufferPtr + pQuantizedScaleFactors);
					double qScaleFactorX = quantizationPtr[0];
					double qScaleFactorY = quantizationPtr[1];
					double qScaleFactorZ = quantizationPtr[2];

					double qOffsetX = quantizationPtr[3];
					double qOffsetY = quantizationPtr[4];
					double qOffsetZ = quantizationPtr[5];

					Quantization = new SQuantization3D(qScaleFactorX, qScaleFactorY, qScaleFactorZ, qOffsetX, qOffsetY, qOffsetZ);

					double maxX = quantizationPtr[6];
					double minX = quantizationPtr[7];
					double maxY = quantizationPtr[8];
					double minY = quantizationPtr[9];
					double maxZ = quantizationPtr[10];
					double minZ = quantizationPtr[11];

					m_headerExtent = new Extent3D(minX, minY, minZ, maxX, maxY, maxZ);

					long pointDataRegionLength = inputLength - PointOffset;
					if (pointDataRegionLength < PointDataRecordLength * m_pointCount)
						throw new Exception("Invalid format: point data region is not the expected size");
				}

				BufferManager.ReleaseBuffer(inputBuffer);
			}
		}

		public override PointCloudBinarySource GenerateBinarySource(ProgressManager progressManager)
		{
			return CreateLASToBinaryWrapper(progressManager);
		}

		public override string GetPreview()
		{
			StringBuilder sb = new StringBuilder();

			sb.AppendLine("LASF");
			sb.AppendLine(String.Format("Points: {0:0,0}", m_pointCount));
			sb.AppendLine(String.Format("Extent: {0}", m_headerExtent));
			sb.AppendLine();
			sb.AppendLine(String.Format("Point Size: {0} bytes", PointDataRecordLength));

			return sb.ToString();
		}

		public unsafe PointCloudBinarySource CreateLASToBinaryWrapper(ProgressManager progressManager)
		{
			Extent3D extent = m_headerExtent;

			if (!TRUST_HEADER_EXTENT)
			{
				Stopwatch stopwatch = new Stopwatch();
				stopwatch.Start();

				int pointCount = 0;
				int bytesRead = 0;

				int minX = 0, minY = 0, minZ = 0;
				int maxX = 0, maxY = 0, maxZ = 0;

				// determine usable buffer size
				short pointDataRecordLength = PointDataRecordLength;
				int pointsPerInputBuffer = BufferManager.BUFFER_SIZE_BYTES / pointDataRecordLength;
				int usableBytesPerInputBuffer = pointsPerInputBuffer * pointDataRecordLength;

				Point3D[] pointBuffer = new Point3D[pointsPerInputBuffer];

				using (FileStream inputStream = new FileStream(FilePath, FileMode.Open, FileAccess.Read, FileShare.None, BufferManager.BUFFER_SIZE_BYTES, FileOptions.SequentialScan))
				{
					long inputLength = inputStream.Length;

					inputStream.Seek(PointOffset, SeekOrigin.Begin);

					byte[] inputBuffer = BufferManager.AcquireBuffer();

					fixed (byte* inputBufferPtr = inputBuffer)
					{
						while ((bytesRead = inputStream.Read(inputBuffer, 0, usableBytesPerInputBuffer)) > 0)
						{
							for (int i = 0; i < bytesRead; i += pointDataRecordLength)
							{
								SQuantizedPoint3D* p = (SQuantizedPoint3D*)(inputBufferPtr + i);

								int x = (*p).X;
								int y = (*p).Y;
								int z = (*p).Z;

								if (pointCount == 0)
								{
									minX = maxX = x;
									minY = maxY = y;
									minZ = maxZ = z;
								}
								else
								{
									if (x < minX) minX = x; else if (x > maxX) maxX = x;
									if (y < minY) minY = y; else if (y > maxY) maxY = y;
									if (z < minZ) minZ = z; else if (z > maxZ) maxZ = z;
								}

								++pointCount;
							}

							if (!progressManager.Update((float)inputStream.Position / inputLength))
								break;
						}
					}

					BufferManager.ReleaseBuffer(inputBuffer);
				}

				progressManager.Log(stopwatch, "Traversed {0:0,0} points", pointCount);

				SQuantizedExtent3D quantizedExtent = new SQuantizedExtent3D(minX, minY, minZ, maxX, maxY, maxZ);
				extent = Quantization.Convert(quantizedExtent);
			}

			PointCloudBinarySource source = new PointCloudBinarySource(FilePath, m_pointCount, extent, Quantization, PointOffset, PointDataRecordLength, CompressionMethod.None);

			return source;
		}
	}
}
