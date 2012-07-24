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

		private readonly long m_count;
		private readonly short m_pointSizeBytes;

		public long Count
		{
			get { return m_count; }
		}

		public short PointSizeBytes
		{
			get { return m_pointSizeBytes; }
		}

		public IEnumerable<string> SourcePaths
		{
			get { return m_files.Select(f => f.FilePath); }
		}

		public IPointCloudBinarySourceEnumerator GetBlockEnumerator(ProgressManagerProcess process)
		{
			return new PointCloudBinarySourceCompositeEnumerator(m_files, process);
		}

		public IPointCloudBinarySourceEnumerator GetBlockEnumerator(BufferInstance buffer)
		{
			throw new NotImplementedException();
		}

		public LASComposite(string path)
			: base(path)
		{
			string baseDirectory = Path.GetDirectoryName(path);

			var files = new List<LASFile>();
			string[] lines = File.ReadAllLines(path);
			foreach (var line in lines)
			{
				string currentPath = line;
				if (!Path.IsPathRooted(currentPath))
					currentPath = Path.Combine(baseDirectory, line);

				if (File.Exists(currentPath))
					files.Add(new LASFile(currentPath));
			}

			// verify that all inputs are compatible
			m_files = files.ToArray();

			m_count = m_files.Sum(f => f.Count);
			m_pointSizeBytes = m_files[0].PointSizeBytes;
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

		public IPointCloudBinarySource CreateLASToBinaryWrapper(ProgressManager progressManager)
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
