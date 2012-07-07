using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

using CloudAE.Core.Geometry;

namespace CloudAE.Core
{
	class LASComposite : FileHandlerBase, IPointCloudBinarySourceEnumerable
	{
		private readonly int m_pointsPerBuffer;
		private readonly int m_usableBytesPerBuffer;

		public long Count
		{
			get { return 0; }
		}

		public long PointDataOffset
		{
			get { return 0; }
		}

		public short PointSizeBytes
		{
			get { return 0; }
		}

		public int UsableBytesPerBuffer
		{
			get { return m_usableBytesPerBuffer; }
		}

		public int PointsPerBuffer
		{
			get { return m_pointsPerBuffer; }
		}

		public IPointCloudBinarySourceEnumerator GetBlockEnumerator(ProgressManagerProcess process)
		{
			return new PointCloudBinarySourceEnumerator(this, process);
		}

		public IPointCloudBinarySourceEnumerator GetBlockEnumerator(BufferInstance buffer)
		{
			return new PointCloudBinarySourceEnumerator(this, buffer);
		}

		public LASComposite(string path)
			: base(path)
		{
			List<LASFile> files = new List<LASFile>();
			string[] lines = File.ReadAllLines(path);
			foreach (var line in lines)
				files.Add(new LASFile(line));

			// verify that all inputs are compatible

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
			var sb = new StringBuilder();

			sb.AppendLine("LAS Composite");

			return sb.ToString();
		}

		public unsafe PointCloudBinarySource CreateLASToBinaryWrapper(ProgressManager progressManager)
		{
			//var source = new PointCloudBinarySourceComposite();

			return null;
		}
	}
}
