using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Jacere.Core;
using Jacere.Core.Geometry;

namespace Jacere.Data.PointCloud
{
	class LASComposite : FileHandlerBase, IPointCloudBinarySourceEnumerable
	{
		private readonly LASFile[] m_files;

		private readonly long m_size;
		private readonly long m_count;
		private readonly short m_pointSizeBytes;

		private Extent3D m_extent;

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
				{
					var handler = HandlerFactory.GetInputHandler(currentPath);
					var compositeHandler = handler as LASComposite;
					if (compositeHandler != null)
					{
						if (compositeHandler.m_files.Length > 0)
							files.AddRange(compositeHandler.m_files);
					}
					else
					{
						var lasHandler = handler as LASFile;
						if (lasHandler != null)
							files.Add(lasHandler);
					}
				}
			}

			m_files = files.ToArray();

			if (m_files.Length == 0)
				throw new Exception("no files loaded for composite");

			// verify that all inputs are compatible
			var templateFile = m_files[0];
			for (int i = 1; i < m_files.Length; i++)
			{
				if (m_files[i].Header.IsCompatible(templateFile.Header))
				{
					throw new Exception("files are not compatible");
				}
			}

			m_count = m_files.Sum(f => f.Count);
			m_pointSizeBytes = m_files[0].PointSizeBytes;
		}

		public override IPointCloudBinarySource GenerateBinarySource(ProgressManager progressManager)
		{
			var sources = new List<IPointCloudBinarySource>();
			foreach (var file in m_files)
				sources.Add(file.GenerateBinarySource(progressManager));

			var extent = sources.Select(s => s.Extent).Union3D();
			var source = new PointCloudBinarySourceComposite(this, extent, sources.ToArray());
			return source;
		}

		public override string GetPreview()
		{
			var quantization = m_files[0].Header.Quantization;

			var sb = new StringBuilder();

			sb.AppendLine("LAS Composite");
			sb.AppendLine(String.Format("Points: {0:0,0}", m_count));
			//sb.AppendLine(String.Format("Extent: {0}", m_header.Extent));
			//sb.AppendLine(String.Format("File Size: {0}", m_size.ToSize()));
			sb.AppendLine();
			sb.AppendLine(String.Format("Point Size: {0} bytes", m_pointSizeBytes));
			sb.AppendLine();
			sb.AppendLine(String.Format("Offset X: {0}", quantization.OffsetX));
			sb.AppendLine(String.Format("Offset Y: {0}", quantization.OffsetY));
			sb.AppendLine(String.Format("Offset Z: {0}", quantization.OffsetZ));
			sb.AppendLine(String.Format("Scale X: {0}", quantization.ScaleFactorX));
			sb.AppendLine(String.Format("Scale Y: {0}", quantization.ScaleFactorY));
			sb.AppendLine(String.Format("Scale Z: {0}", quantization.ScaleFactorZ));

			return sb.ToString();
		}
	}
}
