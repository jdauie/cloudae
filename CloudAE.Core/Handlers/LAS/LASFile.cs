﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using CloudAE.Core.Geometry;

namespace CloudAE.Core
{
	class LASFile : FileHandlerBase, IPointCloudBinarySourceEnumerable
	{
		private const bool TRUST_HEADER_EXTENT = true;
		
		private readonly LASHeader m_header;
		private readonly LASVLR[] m_vlrs;
		private readonly LASEVLR[] m_evlrs;

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

		public IPointCloudBinarySourceEnumerator GetBlockEnumerator(ProgressManagerProcess process)
		{
			return new PointCloudBinarySourceEnumerator(this, process);
		}

		public IPointCloudBinarySourceEnumerator GetBlockEnumerator(BufferInstance buffer)
		{
			return new PointCloudBinarySourceEnumerator(this, buffer);
		}

		public LASFile(string path)
			: base(path)
		{
			using (var stream = StreamManager.OpenReadStream(FilePath))
			{
				using (var reader = new FlexibleBinaryReader(stream, false))
				{
					m_header = reader.ReadLASHeader();
				}

				m_vlrs = m_header.ReadVLRs(stream);
				m_evlrs = m_header.ReadEVLRs(stream);
			}

			int pointSizeBytes = PointSizeBytes;
		}

		public override IPointCloudBinarySource GenerateBinarySource(ProgressManager progressManager)
		{
			return CreateLASToBinaryWrapper(progressManager);
		}

		public override string GetPreview()
		{
			var sb = new StringBuilder();

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
				using (var process = progressManager.StartProcess("CalculateLASExtent"))
				{
					short pointSizeBytes = PointSizeBytes;

					int minX = 0, minY = 0, minZ = 0;
					int maxX = 0, maxY = 0, maxZ = 0;

					foreach (var chunk in GetBlockEnumerator(process))
					{
						if (minX == 0 && maxX == 0)
						{
							SQuantizedPoint3D* p = (SQuantizedPoint3D*)chunk.DataPtr;

							minX = maxX = (*p).X;
							minY = maxY = (*p).Y;
							minZ = maxZ = (*p).Z;
						}

						byte* pb = chunk.DataPtr;
						byte* pbEnd = chunk.DataEndPtr;
						while(pb < pbEnd)
						{
							SQuantizedPoint3D* p = (SQuantizedPoint3D*)pb;

							if ((*p).X < minX) minX = (*p).X; else if ((*p).X > maxX) maxX = (*p).X;
							if ((*p).Y < minY) minY = (*p).Y; else if ((*p).Y > maxY) maxY = (*p).Y;
							if ((*p).Z < minZ) minZ = (*p).Z; else if ((*p).Z > maxZ) maxZ = (*p).Z;

							pb += pointSizeBytes;
						}

						if (!process.Update(chunk))
							break;
					}

					var quantizedExtent = new SQuantizedExtent3D(minX, minY, minZ, maxX, maxY, maxZ);
					extent = m_header.Quantization.Convert(quantizedExtent);

					process.LogTime("Traversed {0:0,0} points", Count);
				}
			}

			var source = new PointCloudBinarySource(FilePath, Count, extent, m_header.Quantization, PointDataOffset, PointSizeBytes);

			return source;
		}
	}
}
