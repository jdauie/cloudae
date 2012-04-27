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
	class LASFile : FileHandlerBase, IPointCloudBinarySourceEnumerable
	{
		private const bool TRUST_HEADER_EXTENT = false;

		private readonly int m_pointsPerBuffer;
		private readonly int m_usableBytesPerBuffer;
		
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

		public int UsableBytesPerBuffer
		{
			get { return m_usableBytesPerBuffer; }
		}

		public int PointsPerBuffer
		{
			get { return m_pointsPerBuffer; }
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
				m_header = reader.ReadLASHeader();
			}

			int pointSizeBytes = PointSizeBytes;
			m_pointsPerBuffer = BufferManager.BUFFER_SIZE_BYTES / pointSizeBytes;
			m_usableBytesPerBuffer = m_pointsPerBuffer * pointSizeBytes;
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
				using (ProgressManagerProcess process = progressManager.StartProcess("CalculateLASExtent"))
				{
					BufferInstance buffer = process.AcquireBuffer(true);

					short pointSizeBytes = PointSizeBytes;

					int minX = 0, minY = 0, minZ = 0;
					int maxX = 0, maxY = 0, maxZ = 0;

					foreach (PointCloudBinarySourceEnumeratorChunk chunk in GetBlockEnumerator(buffer.Data))
					{
						if (minX == 0 && maxX == 0)
						{
							SQuantizedPoint3D* p = (SQuantizedPoint3D*)buffer.DataPtr;

							minX = maxX = (*p).X;
							minY = maxY = (*p).Y;
							minZ = maxZ = (*p).Z;
						}

						byte* pb = buffer.DataPtr;
						byte* pbEnd = pb + chunk.BytesRead;
						while(pb < pbEnd)
						{
							SQuantizedPoint3D* p = (SQuantizedPoint3D*)(pb);
							pb += pointSizeBytes;

							if ((*p).X < minX) minX = (*p).X; else if ((*p).X > maxX) maxX = (*p).X;
							if ((*p).Y < minY) minY = (*p).Y; else if ((*p).Y > maxY) maxY = (*p).Y;
							if ((*p).Z < minZ) minZ = (*p).Z; else if ((*p).Z > maxZ) maxZ = (*p).Z;
						}

						if (!process.Update(chunk))
							break;
					}

					SQuantizedExtent3D quantizedExtent = new SQuantizedExtent3D(minX, minY, minZ, maxX, maxY, maxZ);
					extent = m_header.Quantization.Convert(quantizedExtent);

					process.LogTime("Traversed {0:0,0} points", Count);
				}
			}

			PointCloudBinarySource source = new PointCloudBinarySource(FilePath, Count, extent, m_header.Quantization, PointDataOffset, PointSizeBytes, CompressionMethod.None);

			return source;
		}
	}
}
