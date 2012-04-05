﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Diagnostics;
using CloudAE.Core.Geometry;
using CloudAE.Core.Compression;

namespace CloudAE.Core
{
	class LASFile : FileHandlerBase, IPointCloudBinarySourceEnumerable
	{
		private const bool TRUST_HEADER_EXTENT = false;
		
		private LASHeader m_header;

		public long Count
		{
			get { return (long)m_header.PointCount; }
		}

		public long PointDataOffset
		{
			get { return m_header.OffsetToPointData; }
		}

		public short PointSizeBytes
		{
			get { return (short)m_header.PointDataRecordLength; }
		}

		public PointCloudBinarySourceEnumerator GetBlockEnumerator(byte[] buffer)
		{
			return new PointCloudBinarySourceEnumerator(this, buffer);
		}

		public unsafe LASFile(string path)
			: base(path)
		{
			using (BinaryReader reader = new BinaryReader(File.OpenRead(FilePath)))
			{
				m_header = new LASHeader(reader);
			}
		}

		public override PointCloudBinarySource GenerateBinarySource(ProgressManager progressManager)
		{
			return CreateLASToBinaryWrapper(progressManager);
		}

		public override string GetPreview()
		{
			StringBuilder sb = new StringBuilder();

			sb.AppendLine(LASHeader.FILE_SIGNATURE);
			sb.AppendLine(String.Format("Points: {0:0,0}", m_header.PointCount));
			sb.AppendLine(String.Format("Extent: {0}", m_header.Extent));
			sb.AppendLine(String.Format("File Size: {0}", Size.ToSize()));
			sb.AppendLine();
			sb.AppendLine(String.Format("Point Size: {0} bytes", m_header.PointDataRecordLength));
			sb.AppendLine();
			sb.AppendLine(String.Format("Offset X: {0}", m_header.Quantization.OffsetX));
			sb.AppendLine(String.Format("Offset Y: {0}", m_header.Quantization.OffsetY));
			sb.AppendLine(String.Format("Offset Z: {0}", m_header.Quantization.OffsetZ));
			sb.AppendLine(String.Format("Scale X: {0}", m_header.Quantization.ScaleFactorX));
			sb.AppendLine(String.Format("Scale Y: {0}", m_header.Quantization.ScaleFactorY));
			sb.AppendLine(String.Format("Scale Z: {0}", m_header.Quantization.ScaleFactorZ));

			return sb.ToString();
		}

		public unsafe PointCloudBinarySource CreateLASToBinaryWrapper(ProgressManager progressManager)
		{
			Extent3D extent = m_header.Extent;

			if (!TRUST_HEADER_EXTENT)
			{
				Stopwatch stopwatch = new Stopwatch();
				stopwatch.Start();

				int minX = 0, minY = 0, minZ = 0;
				int maxX = 0, maxY = 0, maxZ = 0;

				byte[] buffer = BufferManager.AcquireBuffer();

				fixed (byte* inputBufferPtr = buffer)
				{
					foreach (PointCloudBinarySourceEnumeratorChunk chunk in GetBlockEnumerator(buffer))
					{
						if (minX == 0 && maxX == 0 && minY == 0 && maxY == 0)
						{
							SQuantizedPoint3D* p = (SQuantizedPoint3D*)inputBufferPtr;

							minX = maxX = (*p).X;
							minY = maxY = (*p).Y;
							minZ = maxZ = (*p).Z;
						}

						for (int i = 0; i < chunk.BytesRead; i += PointSizeBytes)
						{
							SQuantizedPoint3D* p = (SQuantizedPoint3D*)(inputBufferPtr + i);

							int x = (*p).X;
							int y = (*p).Y;
							int z = (*p).Z;

							if (x < minX) minX = x; else if (x > maxX) maxX = x;
							if (y < minY) minY = y; else if (y > maxY) maxY = y;
							if (z < minZ) minZ = z; else if (z > maxZ) maxZ = z;
						}

						if (!progressManager.Update(chunk.EnumeratorProgress))
							break;
					}
				}

				BufferManager.ReleaseBuffer(buffer);

				progressManager.Log(stopwatch, "Traversed {0:0,0} points", Count);

				SQuantizedExtent3D quantizedExtent = new SQuantizedExtent3D(minX, minY, minZ, maxX, maxY, maxZ);
				extent = m_header.Quantization.Convert(quantizedExtent);
			}

			PointCloudBinarySource source = new PointCloudBinarySource(FilePath, Count, extent, m_header.Quantization, PointDataOffset, PointSizeBytes, CompressionMethod.None);

			return source;
		}
	}
}
