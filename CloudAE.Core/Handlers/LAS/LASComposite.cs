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
		private readonly LASFile[] m_files;

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
			{
				if (File.Exists(line))
					files.Add(new LASFile(line));
			}

			// verify that all inputs are compatible
			m_files = files.ToArray();
		}

		public override IPointCloudBinarySource GenerateBinarySource(ProgressManager progressManager)
		{
			return CreateLASToBinaryWrapper(progressManager);
		}

		public override string GetPreview()
		{
			var sb = new StringBuilder();

			sb.AppendLine("LAS Composite");

			return sb.ToString();
		}

		public unsafe IPointCloudBinarySource CreateLASToBinaryWrapper(ProgressManager progressManager)
		{
			var sources = new List<IPointCloudBinarySource>();
			foreach (var file in m_files)
				sources.Add(file.CreateLASToBinaryWrapper(progressManager));

			var extent = sources.Select(s => s.Extent).Union3D();
			var source = new PointCloudBinarySourceComposite(FilePath, extent, sources.ToArray());
			return source;
		}
	}
}
