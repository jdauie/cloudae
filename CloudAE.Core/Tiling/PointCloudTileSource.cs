using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Media.Imaging;

using CloudAE.Core;
using CloudAE.Core.Compression;
using CloudAE.Core.Delaunay;
using CloudAE.Core.DelaunayIncremental;
using CloudAE.Core.Geometry;
using CloudAE.Core.Util;

namespace CloudAE.Core
{
	public class PointCloudTileSource : PointCloudBinarySource, ISerializeBinary, INotifyPropertyChanged
	{
		public const string FILE_EXTENSION = "tpb";
		
		private const int MAX_PREVIEW_DIMENSION = 1000;

		private const string FILE_IDENTIFIER = "TPBF";
		private const string FILE_IDENTIFIER_DIRTY = "TPBD";
		private const int FILE_VERSION_MAJOR = 1;
		private const int FILE_VERSION_MINOR = 10;

		private readonly PointCloudTileSet m_tileSet;
		private readonly Statistics m_statisticsZ;
		
		private UQuantizedExtent3D m_quantizedExtent;

		private FileStream m_inputStream;
		private bool m_isDirty;

		private Grid<float> m_pixelGrid;
		private PreviewImage m_preview;

		private System.Windows.Media.Imaging.BitmapImage m_icon;

		#region INotifyPropertyChanged Members

		public event PropertyChangedEventHandler PropertyChanged;

		protected void OnPropertyChanged(string name)
		{
			PropertyChangedEventHandler handler = PropertyChanged;
			if (handler != null)
				handler(this, new PropertyChangedEventArgs(name));
		}

		#endregion

		#region Properties

		public System.Windows.Media.ImageSource Icon
		{
			get
			{
				if (m_icon == null)
					m_icon = new System.Windows.Media.Imaging.BitmapImage(new Uri("pack://application:,,,/CloudAE.Core;component/Icons/bullet_green.png"));
				return m_icon;
			}
		}

		public PointCloudTileSet TileSet
		{
			get { return m_tileSet; }
		}

		public Statistics StatisticsZ
		{
			get { return m_statisticsZ; }
		}

		public BitmapSource PreviewImage
		{
			get { return m_preview != null ? m_preview.Image : null; }
		}

		public PreviewImage Preview
		{
			get { return m_preview; }
			private set
			{
				if (m_preview != value)
				{
					m_preview = value;
					OnPropertyChanged("PreviewImage");
				}
			}
		}

		public Grid<float> PixelGrid
		{
			get { return m_pixelGrid; }
		}

		public UQuantizedExtent3D QuantizedExtent
		{
			get { return m_quantizedExtent; }
			set
			{
				m_quantizedExtent = value;
				Extent = Quantization.Convert(m_quantizedExtent);
			}
		}

		public Point3D CenterOfMass
		{
			get
			{
				return new Point3D(Extent.MidpointX, Extent.MidpointY, StatisticsZ.ModeApproximate);
			}
		}

		public bool IsDirty
		{
			get { return m_isDirty; }
			set { m_isDirty = value; }
		}

		public override string Name
		{
			get
			{
				string name = base.Name;
				name = Path.GetFileNameWithoutExtension(name);
				name = Path.GetFileNameWithoutExtension(name);

				return name;
			}
		}

		#endregion

		public PointCloudTileSource(string file, PointCloudTileSet tileSet, Quantization3D quantization, short pointSizeBytes, Statistics zStats, CompressionMethod compression)
			: this(file, tileSet, quantization, 0, pointSizeBytes, zStats, compression)
		{
		}

		public PointCloudTileSource(string file, PointCloudTileSet tileSet, Quantization3D quantization, long pointDataOffset, short pointSizeBytes, Statistics zStats, CompressionMethod compression)
			: base(file, tileSet.PointCount, tileSet.Extent, quantization, pointDataOffset, pointSizeBytes, compression)
		{
			m_tileSet = new PointCloudTileSet(tileSet, this);
			m_statisticsZ = zStats;
			QuantizedExtent = (UQuantizedExtent3D)Quantization.Convert(Extent);

			if (pointDataOffset == 0)
			{
				IsDirty = true;
				WriteHeader();
			}
		}

		public static string GetTileSourcePath(string path)
		{
			// mark with some low-order bytes of the file size
			FileInfo fileInfo = new FileInfo(path);
			string fileName = String.Format("{0}.{1}.{2}", fileInfo.Name, EncodingConverter.ToBase64SafeString(BitConverter.GetBytes(fileInfo.Length), 0, 3), PointCloudTileSource.FILE_EXTENSION);
			string tilePath = Path.Combine(Cache.APP_CACHE_DIR, fileName);
			return tilePath;
		}

		public static PointCloudTileSource Open(string file)
		{
			long pointDataOffset;
			short pointSizeBytes;
			CompressionMethod compression;

			UQuantization3D quantization;
			Statistics zStats;
			PointCloudTileSet tileSet;

			using (BinaryReader reader = new BinaryReader(File.OpenRead(file)))
			{
				if (ASCIIEncoding.ASCII.GetString(reader.ReadBytes(FILE_IDENTIFIER.Length)) != FILE_IDENTIFIER)
					throw new OpenFailedException(file, "File identifier does not match.");

				int versionMajor = reader.ReadInt32();
				int versionMinor = reader.ReadInt32();

				if (versionMajor != FILE_VERSION_MAJOR || versionMinor != FILE_VERSION_MINOR)
					throw new OpenFailedException(file, "File version does not match.");

				pointDataOffset = reader.ReadInt64();
				pointSizeBytes = reader.ReadInt16();
				compression = (CompressionMethod)reader.ReadInt32();

				quantization = reader.ReadUQuantization3D();
				zStats = reader.ReadStatistics();
				tileSet = reader.ReadTileSet();
			}

			PointCloudTileSource source = new PointCloudTileSource(file, tileSet, quantization, pointDataOffset, pointSizeBytes, zStats, compression);

			return source;
		}

		public void Serialize(BinaryWriter writer)
		{
			// assumes this function will be atomic, which is true for cancellation, 
			// but not for exceptions during the write operation
			string identifier = IsDirty ? FILE_IDENTIFIER_DIRTY : FILE_IDENTIFIER;

			writer.Write(ASCIIEncoding.ASCII.GetBytes(identifier));
			writer.Write(FILE_VERSION_MAJOR);
			writer.Write(FILE_VERSION_MINOR);
			writer.Write(PointDataOffset);
			writer.Write(PointSizeBytes);
			writer.Write((int)Compression);
			writer.Write(Quantization);
			writer.Write(StatisticsZ);
			writer.Write(TileSet);
		}

		public void WriteHeader()
		{
			Close();

			using (BinaryWriter writer = new BinaryWriter(File.OpenWrite(FilePath)))
			{
				writer.Write(this);

				if (PointDataOffset == 0)
				{
					PointDataOffset = (int)writer.BaseStream.Position;

					// serialize again to write out the correct point offset
					// (obviously I should do this a better way)
					writer.Seek(0, SeekOrigin.Begin);
					writer.Write(this);
				}
			}
		}

		public void Open()
		{
			if (m_inputStream == null)
			{
				m_inputStream = new FileStream(FilePath, FileMode.Open, FileAccess.Read, FileShare.None, BufferManager.BUFFER_SIZE_BYTES, FileOptions.RandomAccess);
			}
		}

		public void OpenSequential()
		{
			if (m_inputStream == null)
			{
				m_inputStream = new FileStream(FilePath, FileMode.Open, FileAccess.Read, FileShare.None, BufferManager.BUFFER_SIZE_BYTES, FileOptions.SequentialScan);
			}
		}

		public void Close()
		{
			if (m_inputStream != null)
			{
				m_inputStream.Dispose();
				m_inputStream = null;
			}
		}

		public unsafe void AllocateFile(bool allowSparse)
		{
			long outputLength = (long)Count * PointSizeBytes + PointDataOffset;
			{
				Stopwatch stopwatch = new Stopwatch();
				stopwatch.Start();

				// this works great when it fits in file cache
				// but not when I am breaking it into segments
				// unfortunately, segments are not apparent at this level
				//File.Copy(source.FilePath, path);

				// this might be about twice as fast for massive files
				using (FileStream outputStream = new FileStream(FilePath, FileMode.OpenOrCreate, FileAccess.Write, FileShare.None, BufferManager.BUFFER_SIZE_BYTES, FileOptions.SequentialScan))
				{
					outputStream.SetLength(outputLength);

					if (!allowSparse)
					{
						outputStream.Seek(outputLength - 1, SeekOrigin.Begin);
						outputStream.WriteByte(1);
					}
				}

				stopwatch.Stop();

				Context.WriteLine("Allocated {1}MB in {0}ms", stopwatch.ElapsedMilliseconds, outputLength / (int)ByteSizesSmall.MB_1);
			}
		}

		public unsafe void LoadTile(PointCloudTile tile, byte[] inputBuffer)
		{
			if (tile.PointCount == 0)
				return;

			Open();

			int bytesRead = tile.ReadTile(m_inputStream, inputBuffer);

			// usage
			//fixed (byte* inputBufferPtr = inputBuffer)
			//{
			//    UQuantizedPoint3D* p = (UQuantizedPoint3D*)inputBufferPtr;

			//    for (int i = 0; i < tile.PointCount; i++)
			//    {
			//        //// slow!
			//        //Point3D point = Quantization.Convert(p[i]);
			//        //if (point.Z == double.MaxValue)
			//        //    (*p).Z = 0;
			//    }
			//}
		}

		public unsafe System.Windows.Media.Media3D.MeshGeometry3D LoadTilePointMesh(PointCloudTile tile, byte[] inputBuffer, double pointSize, int thinByFactor)
		{
			LoadTile(tile, inputBuffer);

			Extent3D distributionExtent = tile.Extent;
			Extent3D centeringExtent = Extent;
			Point3D centerOfMass = CenterOfMass;

			double xShift = -centeringExtent.MidpointX;
			double yShift = -centeringExtent.MidpointY;
			double zShift = -centerOfMass.Z;

			double halfPointSize = pointSize / 2;

			int thinnedTilePointCount = tile.PointCount / thinByFactor;

			// these values need to be changed from 4,6 to 8,36 if I test it out
			System.Windows.Media.Media3D.Point3DCollection positions = new System.Windows.Media.Media3D.Point3DCollection(thinnedTilePointCount * 4);
			System.Windows.Media.Media3D.Vector3DCollection normals = new System.Windows.Media.Media3D.Vector3DCollection(positions.Count);
			System.Windows.Media.Int32Collection indices = new System.Windows.Media.Int32Collection(tile.PointCount * 6);

			fixed (byte* inputBufferPtr = inputBuffer)
			{
				UQuantizedPoint3D* p = (UQuantizedPoint3D*)inputBufferPtr;

				for (int i = 0; i < tile.PointCount; i++)
				{
					if (!(i % thinByFactor == 0))
						continue;

					// slow!
					Point3D point = Quantization.Convert(p[i]);
					double xC = point.X + xShift;
					double yC = point.Y + yShift;
					double zC = point.Z + zShift;

					int currentStartIndex = positions.Count;

					foreach (double y in new double[] { yC - halfPointSize, yC + halfPointSize })
					{
						foreach (double x in new double[] { xC - halfPointSize, xC + halfPointSize })
						{
							positions.Add(new System.Windows.Media.Media3D.Point3D(x, y, zC));
							normals.Add(new System.Windows.Media.Media3D.Vector3D(0, 0, 1));
						}
					}

					indices.Add(currentStartIndex + 0);
					indices.Add(currentStartIndex + 1);
					indices.Add(currentStartIndex + 3);

					indices.Add(currentStartIndex + 0);
					indices.Add(currentStartIndex + 3);
					indices.Add(currentStartIndex + 2);
				}
			}

			System.Windows.Media.Media3D.MeshGeometry3D geometry = new System.Windows.Media.Media3D.MeshGeometry3D();
			geometry.Positions = positions;
			geometry.TriangleIndices = indices;
			geometry.Normals = normals;

			return geometry;
		}

		public Point3D[] GetPointsWithinRegion(Polygon2D polygon, bool byRatio)
		{
			return null;
		}

		public Point3D[] GetPointsNearLine(System.Windows.Point p0, System.Windows.Point p1, double distance, bool byRatio)
		{
			// convert ratio points to quantized points?
			// create a region from the points and the distance param
			// figure out which tiles intersect the region
			// load tile points and check whether they are contained in the region
			return null;
		}

		public KeyValuePair<Grid<uint>, Grid<float>> GenerateGrid(PointCloudTile template, ushort maxDimension)
		{
			Extent3D extent = template.Extent;
			UQuantizedExtent2D quantizedExtent = template.QuantizedExtent;

			float fillVal = (float)extent.MinZ - 1;
			Grid<float> grid = new Grid<float>(extent, 2, maxDimension, fillVal, true);
			Grid<uint> quantizedGrid = new Grid<uint>(grid.SizeX, grid.SizeY, extent, true);

			return new KeyValuePair<Grid<uint>, Grid<float>>(quantizedGrid, grid);
		}

		public unsafe void LoadTileGrid(PointCloudTile tile, byte[] inputBuffer, Grid<float> grid, Grid<uint> quantizedGrid)
		{
			Open();

			UQuantizedExtent2D quantizedExtent = tile.QuantizedExtent;

			double cellSizeX = (double)quantizedExtent.RangeX / grid.SizeX;
			double cellSizeY = (double)quantizedExtent.RangeY / grid.SizeY;

			grid.FillVal = (float)tile.Extent.MinZ - 1;
			grid.Reset();
			quantizedGrid.Reset();

			fixed (byte* inputBufferPtr = inputBuffer)
			{
				int bytesRead = tile.ReadTile(m_inputStream, inputBuffer);

				byte* pb = inputBufferPtr;
				byte* pbEnd = inputBufferPtr + tile.StorageSize;
				while (pb < pbEnd)
				{
					UQuantizedPoint3D* p = (UQuantizedPoint3D*)(pb);
					pb += PointSizeBytes;

					int pixelX = (int)(((*p).X - quantizedExtent.MinX) / cellSizeX);
					int pixelY = (int)(((*p).Y - quantizedExtent.MinY) / cellSizeY);

					if ((*p).Z > quantizedGrid.Data[pixelX, pixelY])
						quantizedGrid.Data[pixelX, pixelY] = (*p).Z;
				}
			}

			quantizedGrid.CorrectMaxOverflow();
			quantizedGrid.CopyToUnquantized(grid, Quantization, Extent);
		}

		public System.Windows.Media.Media3D.MeshGeometry3D GenerateMesh(Grid<float> grid, Extent3D distributionExtent)
		{
			return GenerateMesh(grid, distributionExtent, false);
		}

		public System.Windows.Media.Media3D.MeshGeometry3D GenerateMesh(Grid<float> grid, Extent3D distributionExtent, bool showBackFaces)
		{
			// subtract midpoint to center around (0,0,0)
			Extent3D centeringExtent = Extent;
			Point3D centerOfMass = CenterOfMass;

			System.Windows.Media.Media3D.Point3DCollection positions = new System.Windows.Media.Media3D.Point3DCollection(grid.CellCount);
			System.Windows.Media.Int32Collection indices = new System.Windows.Media.Int32Collection(2 * (grid.SizeX - 1) * (grid.SizeY - 1));
			
			float fillVal = grid.FillVal;

			for (int x = 0; x < grid.SizeX; x++)
			{
				for (int y = 0; y < grid.SizeY; y++)
				{
					double value = grid.Data[x, y] - centerOfMass.Z;

					double xCoord = ((double)x / grid.SizeX) * distributionExtent.RangeX + distributionExtent.MinX - distributionExtent.MidpointX;
					double yCoord = ((double)y / grid.SizeY) * distributionExtent.RangeY + distributionExtent.MinY - distributionExtent.MidpointY;

					xCoord += (distributionExtent.MidpointX - centeringExtent.MidpointX);
					yCoord += (distributionExtent.MidpointY - centeringExtent.MidpointY);

					System.Windows.Media.Media3D.Point3D point = new System.Windows.Media.Media3D.Point3D(xCoord, yCoord, value);
					positions.Add(point);

					if (x > 0 && y > 0)
					{
						// add two triangles
						int currentPosition = x * grid.SizeY + y;
						int topPosition = currentPosition - 1;
						int leftPosition = currentPosition - grid.SizeY;
						int topleftPosition = leftPosition - 1;

						if (grid.Data[x - 1, y] != fillVal && grid.Data[x, y - 1] != fillVal)
						{
							if (grid.Data[x, y] != fillVal)
							{
								indices.Add(leftPosition);
								indices.Add(topPosition);
								indices.Add(currentPosition);

								if (showBackFaces)
								{
									indices.Add(leftPosition);
									indices.Add(currentPosition);
									indices.Add(topPosition);
								}
							}

							if (grid.Data[x - 1, y - 1] != fillVal)
							{
								indices.Add(topleftPosition);
								indices.Add(topPosition);
								indices.Add(leftPosition);

								if (showBackFaces)
								{
									indices.Add(topleftPosition);
									indices.Add(leftPosition);
									indices.Add(topPosition);
								}
							}
						}
					}
				}
			}

			System.Windows.Media.Media3D.Vector3DCollection normals = new System.Windows.Media.Media3D.Vector3DCollection(positions.Count);

			for (int i = 0; i < positions.Count; i++)
				normals.Add(new System.Windows.Media.Media3D.Vector3D(0, 0, 0));

			for (int i = 0; i < indices.Count; i += 3)
			{
				int index1 = indices[i];
				int index2 = indices[i + 1];
				int index3 = indices[i + 2];

				System.Windows.Media.Media3D.Vector3D side1 = positions[index1] - positions[index3];
				System.Windows.Media.Media3D.Vector3D side2 = positions[index1] - positions[index2];
				System.Windows.Media.Media3D.Vector3D normal = System.Windows.Media.Media3D.Vector3D.CrossProduct(side1, side2);

				normals[index1] += normal;
				normals[index2] += normal;
				normals[index3] += normal;
			}

			for (int i = 0; i < normals.Count; i++)
			{
				if (normals[i].Length > 0)
				{
					System.Windows.Media.Media3D.Vector3D normal = normals[i];
					normal.Normalize();

					// the fact that this is necessary means I am doing something wrong
					if (normal.Z < 0)
						normal.Negate();

					normals[i] = normal;
				}
			}
			
			System.Windows.Media.Media3D.MeshGeometry3D geometry = new System.Windows.Media.Media3D.MeshGeometry3D();
			geometry.Positions = positions;
			geometry.TriangleIndices = indices;
			geometry.Normals = normals;

			return geometry;
		}

		public unsafe System.Windows.Media.Media3D.MeshGeometry3D LoadTileMeshDelaunayIncremental(PointCloudTile tile, byte[] inputBuffer)
		{
			Open();

			Extent3D extent = tile.Extent;
			
			DelaunayPoint[] pointsToTriangulate = new DelaunayPoint[tile.PointCount];

			fixed (byte* inputBufferPtr = inputBuffer)
			{
				UQuantizedPoint3D* p = (UQuantizedPoint3D*)inputBufferPtr;

				int bytesRead = tile.ReadTile(m_inputStream, inputBuffer);

				for (int i = 0; i < tile.PointCount; i++)
				{
					Point3D point = Quantization.Convert(p[i]);

					pointsToTriangulate[i] = new DelaunayPoint(
						(float)point.X,
						(float)point.Y,
						(float)point.Z,
						i);
				}
			}

			Delaunay2DIncremental triangulator = new Delaunay2DIncremental();
			triangulator.Initialize(extent, pointsToTriangulate.Length);

			foreach (DelaunayPoint point in pointsToTriangulate)
			{
				if (triangulator.Locate(point))
					triangulator.UpdateTriangle(point);
			}

			List<int> mesh = triangulator.FlushTriangles();

			System.Windows.Media.Media3D.Point3DCollection points = new System.Windows.Media.Media3D.Point3DCollection(pointsToTriangulate.Length);
			for (int i = 0; i < pointsToTriangulate.Length; i++)
				points.Add(new System.Windows.Media.Media3D.Point3D(
					pointsToTriangulate[i].X - Extent.MidpointX,
					pointsToTriangulate[i].Y - Extent.MidpointY,
					pointsToTriangulate[i].Z - Extent.MidpointZ));

			System.Windows.Media.Int32Collection triangles = new System.Windows.Media.Int32Collection(mesh.Reverse<int>());

			System.Windows.Media.Media3D.MeshGeometry3D meshGeometry = new System.Windows.Media.Media3D.MeshGeometry3D();
			meshGeometry.Positions = points;
			meshGeometry.TriangleIndices = triangles;

			return meshGeometry;
		}

		public unsafe KeyValuePair<System.Windows.Media.Media3D.Point3DCollection, System.Windows.Media.Int32Collection> LoadTileMeshSHullBroken(PointCloudTile tile, byte[] inputBuffer)
		{
			Open();

			Vertex[] pointsToTriangulate = new Vertex[tile.PointCount];

			fixed (byte* inputBufferPtr = inputBuffer)
			{
				UQuantizedPoint3D* p = (UQuantizedPoint3D*)inputBufferPtr;

				int bytesRead = tile.ReadTile(m_inputStream, inputBuffer);

				for (int i = 0; i < tile.PointCount; i++)
				{
					Point3D point = Quantization.Convert(p[i]);

					pointsToTriangulate[i] = new Vertex(
						(float)point.X,
						(float)point.Y,
						(float)point.Z);
				}
			}

			Triangulator triangulator = new Triangulator();
			List<Triad> mesh = triangulator.Triangulation(pointsToTriangulate, true);

			System.Windows.Media.Media3D.Point3DCollection points = new System.Windows.Media.Media3D.Point3DCollection(pointsToTriangulate.Length);
			for (int i = 0; i < pointsToTriangulate.Length; i++)
				points.Add(new System.Windows.Media.Media3D.Point3D(
					pointsToTriangulate[i].x - Extent.MidpointX,
					pointsToTriangulate[i].y - Extent.MidpointY,
					pointsToTriangulate[i].z - Extent.MidpointZ));

			System.Windows.Media.Int32Collection triangles = new System.Windows.Media.Int32Collection(3 * mesh.Count);
			for (int i = 0; i < mesh.Count; i++)
			{
				triangles.Add(mesh[i].a);
				triangles.Add(mesh[i].b);
				triangles.Add(mesh[i].c);
			}

			return new KeyValuePair<System.Windows.Media.Media3D.Point3DCollection, System.Windows.Media.Int32Collection>(points, triangles);
		}

		public unsafe PointCloudTileSource CompressTileSource(CompressionMethod compressionMethod, ProgressManager progressManager)
		{
			int maxIndividualBufferSize = TileSet.Density.MaxTileCount * PointSizeBytes;
			//if (maxIndividualBufferSize > BufferManager.BUFFER_SIZE_BYTES)
			//    throw new Exception("tile size was not anticipated to be larger than buffer size");

			Stopwatch stopwatch = new Stopwatch();
			stopwatch.Start();

			string outputTempFile = ProcessingSet.GetTemporaryCompressedTileSourceName(FilePath);

			PointCloudTileSource tempTileSource = new PointCloudTileSource(outputTempFile, TileSet, Quantization, PointSizeBytes, StatisticsZ, compressionMethod);

			double compressionRatio = 0.0;
			double pretendCompressionRatio = 0.0;

			long compressedCount = 0;

			byte[] inputBuffer = new byte[maxIndividualBufferSize];
			byte[] outputBuffer = new byte[maxIndividualBufferSize];

			ICompressor compressor = CompressionFactory.GetCompressor(compressionMethod);

			using (FileStream inputStream = new FileStream(FilePath, FileMode.Open, FileAccess.Read, FileShare.Read, BufferManager.BUFFER_SIZE_BYTES, FileOptions.SequentialScan))
			using (FileStream outputStream = new FileStream(outputTempFile, FileMode.Create, FileAccess.Write, FileShare.None, BufferManager.BUFFER_SIZE_BYTES, FileOptions.SequentialScan | FileOptions.WriteThrough))
			{
				long inputLength = inputStream.Length;
				outputStream.SetLength(inputLength);
				outputStream.Seek(PointDataOffset, SeekOrigin.Begin);

				fixed (byte* inputBufferPtr = inputBuffer)
				{
					UQuantizedPoint3D* p = (UQuantizedPoint3D*)inputBufferPtr;

					foreach (PointCloudTile tile in this.TileSet.ValidTiles)
					{
						int bytesRead = tile.ReadTile(inputStream, inputBuffer);

						bool sortComponent = true;
						bool sortGrid = false;
						bool checkBitCompaction = true;

						UQuantizedExtent3D qExtent = tile.QuantizedExtent;
						uint rangeX = qExtent.RangeX;
						uint rangeY = qExtent.RangeY;
						uint rangeZ = qExtent.RangeZ;

						// remove offsets
						for (int i = 0; i < tile.PointCount; i++)
						{
							p[i].X -= qExtent.MinX;
							p[i].Y -= qExtent.MinY;
							p[i].Z -= qExtent.MinZ;
						}

						if (sortGrid)
						{
							SortOnGrid(p, tile.PointCount, qExtent);

							rangeX = 0;
							rangeY = 0;

							for (int i = 0; i < tile.PointCount; i++)
							{
								rangeX = Math.Max(rangeX, p[i].X);
								rangeY = Math.Max(rangeY, p[i].Y);
							}
						}

						if (sortComponent)
						{
							rangeX = SortAndDeltaEncode(p, tile.PointCount);
						}

						if (checkBitCompaction)
						{
							// check the bit-compaction
							int xBits = (int)Math.Ceiling(Math.Log(rangeX, 2));
							int yBits = (int)Math.Ceiling(Math.Log(rangeY, 2));
							int zBits = (int)Math.Ceiling(Math.Log(rangeZ, 2));

							pretendCompressionRatio += (double)tile.PointCount * (xBits + yBits + zBits) / (PointSizeBytes * 8);
						}

						//// check the values
						//List<uint> xVals = new List<uint>(tile.PointCount);
						//List<uint> yVals = new List<uint>(tile.PointCount);
						//List<uint> zVals = new List<uint>(tile.PointCount);
						//for (int i = 0; i < tile.PointCount; i++)
						//{
						//    xVals.Add(p[i].X);
						//    yVals.Add(p[i].Y);
						//    zVals.Add(p[i].Z);
						//}


						//Dictionary<uint, long> valueProbabilityCounts = new Dictionary<uint, long>();

						//for (int i = 0; i < tile.PointCount; i++)
						//{
						//    uint value = p[i].Z;
						//    if (!valueProbabilityCounts.ContainsKey(value))
						//        valueProbabilityCounts.Add(value, 0);
						//    ++valueProbabilityCounts[value];
						//}

						//var sortedValues = valueProbabilityCounts.OrderBy(kvp => kvp.Value).ToArray();



						int compressedSize = bytesRead;

						if (compressor != null)
							compressedSize = compressor.Compress(tile, inputBuffer, bytesRead, outputBuffer);

						byte[] bufferToWrite = outputBuffer;
						int bytesToWrite = compressedSize;

						if (compressedSize >= bytesRead)
						{
							bytesToWrite = bytesRead;
							bufferToWrite = inputBuffer;
						}

						// make new tile object with compressed offset/size
						TileSet[tile.Col, tile.Row] = new PointCloudTile(tile, compressedCount, bytesToWrite);

						// write out buffer (same file or different file?)
						outputStream.Write(bufferToWrite, 0, bytesToWrite);

						compressedCount += bytesToWrite;

						if (!progressManager.Update(tile))
							break;
					}
				}

				outputStream.SetLength(outputStream.Position);

				compressionRatio = (double)compressedCount / inputLength;
				pretendCompressionRatio /= Count;
			}

			tempTileSource.IsDirty = false;
			tempTileSource.WriteHeader();

			stopwatch.Stop();
			progressManager.Log(stopwatch, "Compressed tiles ({0:f}) ({1:f})", compressionRatio, pretendCompressionRatio);

			File.Delete(FilePath);
			File.Move(outputTempFile, FilePath);

			PointCloudTileSource newTileSource = PointCloudTileSource.Open(FilePath);

			return newTileSource;
		}

		private void golombEncode(string source, string dest)
		{
			// limited to Rice coding
			//const int M = 2;
			//const int log2M = 1;

			//IntReader intreader(source);
			//BitWriter bitwriter(dest);
			//while(intreader.hasLeft())
			//{
			//    int num = intreader.getInt();
			//    int q = num / M;
			//    for (int i = 0 ; i < q; i++)
			//    bitwriter.putBit(true);   // write q ones
			//    bitwriter.putBit(false);      // write one zero
			//    int v = 1;
			//    for (int i = 0 ; i < log2M; i++)
			//    {            
			//    bitwriter.putBit( v & num );  
			//    v = v << 1;         
			//    }
			//}
			//bitwriter.close();
			//intreader.close();
		}

		void golombDecode(string source, string dest, int M)
		{
			//BitReader bitreader(source);
			//IntWriter intwriter(dest);
			//int q = 0;
			//int nr = 0;
			//while (bitreader.hasLeft())
			//{
			//nr = 0;
			//q = 0;
			//while (bitreader.getBit()) q++;     // potentially dangerous with malformed files.
			//for (int a = 0; a < log2(M); a++)   // read out the sequential log2(M) bits
			//if (bitreader.getBit())
			//nr += 1 << a;
			//nr += q*M;                          // add the bits and the multiple of M
			//intwriter.putInt(nr);               // write out the value
			//}
			//bitreader.close();
			//intwriter.close();
		}

		private unsafe void QuickSort(UQuantizedPoint3D* a, int i, int j)
		{
			if (i < j)
			{
				int q = Partition(a, i, j);
				QuickSort(a, i, q);
				QuickSort(a, q + 1, j);
			}
		}

		private unsafe int Partition(UQuantizedPoint3D* a, int p, int r)
		{
			int i = p - 1;
			int j = r + 1;
			UQuantizedPoint3D tmp;
			while (true)
			{
				do
				{
					--j;
				} while (a[j].X > a[p].X);
				//} while (a[j].Z > a[p].Z);
				do
				{
					++i;
				} while (a[i].X < a[p].X);
				//} while (a[i].Z < a[p].Z);
				if (i < j)
				{
					tmp = a[i];
					a[i] = a[j];
					a[j] = tmp;
				}
				else return j;
			}
		}

		private unsafe int[,] SortOnGrid(UQuantizedPoint3D* p, int count, UQuantizedExtent3D quantizedExtent)
		{
			UQuantizedPoint3D[] points = new UQuantizedPoint3D[count];
			for (int i = 0; i < count; i++)
				points[i] = p[i];

			int gridSize = 32;
			int gridCellDimensionX = (int)Math.Ceiling((double)quantizedExtent.RangeX / gridSize);
			int gridCellDimensionY = (int)Math.Ceiling((double)quantizedExtent.RangeY / gridSize);

			UQuantizedPoint3DGridComparer comparer = new UQuantizedPoint3DGridComparer(gridSize, gridCellDimensionX, gridCellDimensionY);
			Array.Sort<UQuantizedPoint3D>(points, comparer);

			// find the cells
			// (obviously, this can be optimized)
			int x = 0;
			int y = 0;
			int[,] cellStartIndices = new int[gridSize, gridSize];
			for (int i = 0; i <= count; i++)
			{
				int currentCellX = -1;
				int currentCellY = -1;

				if (i < count)
				{
					currentCellX = (int)(points[i].X / gridCellDimensionX);
					currentCellY = (int)(points[i].Y / gridCellDimensionY);

					if (currentCellX == gridSize) --currentCellX;
					if (currentCellY == gridSize) --currentCellY;
				}

				if (currentCellX != x || currentCellY != y)
				{
					// adjust the previous values
					uint cellMinX = (uint)(gridCellDimensionX * x);
					uint cellMinY = (uint)(gridCellDimensionY * y);
					uint lastZ = 0;
					for (int j = cellStartIndices[x, y]; j < i; j++)
					{
						uint diff = points[j].Z - lastZ;
						lastZ = points[j].Z;

						points[j].X -= cellMinX;
						points[j].Y -= cellMinY;
						points[j].Z = diff;
					}

					if (i < count)
					{
						x = currentCellX;
						y = currentCellY;

						cellStartIndices[x, y] = i;
					}
				}
			}

			for (int i = 0; i < count; i++)
				p[i] = points[i];

			return cellStartIndices;
		}

		private unsafe uint SortAndDeltaEncode(UQuantizedPoint3D* p, int count)
		{
			QuickSort(p, 0, count - 1);

			// delta encoding on single component
			// the first value of the delta may be large, so it can be considered separately

			uint maxX = 0;
			uint lastX = 0;
			for (int i = 0; i < count; i++)
			{
				uint diff = p[i].X - lastX;
				lastX = p[i].X;
				p[i].X = diff;
				if (i > 0 && diff > maxX) maxX = diff;
			}

			return maxX;
		}

		public BitmapSource GeneratePreviewImage(ColorRamp ramp, bool useStdDevStretch)
		{
			if (Preview == null || Preview.ColorHandler != ramp || Preview.UseStdDevStretch != useStdDevStretch)
			{
				BitmapSource source = GeneratePreviewImage(m_pixelGrid, ramp, useStdDevStretch);
				Preview = new PreviewImage(source, ramp, useStdDevStretch);
			}
			return Preview.Image;
		}

		private BitmapSource GeneratePreviewImage(Grid<float> grid, ColorRamp ramp, bool useStdDevStretch)
		{
			BitmapSource bmp = CreateBitmapSource(grid, Extent.RangeZ, useStdDevStretch, ramp);
			//BitmapSource bmp = CreateSegmentationBitmap(grid);
			//BitmapSource bmp = CreatePlaneFittingBitmap(grid);

			return bmp;
		}

		private BitmapSource CreatePlaneFittingBitmap(Grid<float> grid)
		{
			float[,] gridValues = new float[grid.SizeX, grid.SizeY];

			ComputeGridSlopeSurfaceComponent(grid, gridValues);

			for (int x = 0; x < grid.SizeX; x++)
				for (int y = 0; y < grid.SizeY; y++)
					grid.Data[x, y] = gridValues[x, y];

			BitmapSource bmp = CreateBitmapSource(grid, 1.0, false, null);

			return bmp;
		}

		private static void ComputeGridSlopeSurfaceComponent(Grid<float> grid, float[,] gridValues)
		{
			float xMultiplier = (float)(grid.Extent.RangeX / grid.SizeX);
			float yMultiplier = (float)(grid.Extent.RangeY / grid.SizeY);

			for (int x = 1; x < grid.SizeX; x++)
			{
				for (int y = 1; y < grid.SizeY; y++)
				{
					if (grid.Data[x, y] != grid.FillVal)
					{
						Point3D cu = new Point3D(x * xMultiplier, y * yMultiplier, grid.Data[x, y]);
						Point3D tp = new Point3D(x * xMultiplier, (y - 1) * yMultiplier, grid.Data[x, y - 1]);
						Point3D lf = new Point3D((x - 1) * xMultiplier, y * yMultiplier, grid.Data[x - 1, y]);
						Point3D tl = new Point3D((x - 1) * xMultiplier, (y - 1) * yMultiplier, grid.Data[x - 1, y - 1]);

						Plane plane0 = new Plane(cu, tp, tl, true);
						Plane plane1 = new Plane(cu, lf, tl, true);
						//gridValues[x, y] = (float)(plane0.UnitNormal.Z);
						gridValues[x, y] = (float)(Math.Max(plane0.UnitNormal.Z, plane1.UnitNormal.Z));
					}
					else
					{
						gridValues[x, y] = 1;
					}
				}
			}
		}

		private BitmapSource CreateSegmentationBitmap(Grid<float> grid)
		{
			uint[,] gridClasses = new uint[grid.SizeX, grid.SizeY];

			uint currentClassIndex = Segmentation.ClassifyTile(grid, gridClasses, 1.00f, 1, 10, grid.FillVal, NeighborhoodType.FourNeighbors);

			for (int x = 0; x < grid.SizeX; x++)
				for (int y = 0; y < grid.SizeY; y++)
					grid.Data[x, y] = gridClasses[x, y];

			BitmapSource bmp = CreateBitmapSource(grid, currentClassIndex, false, new ColorMapDistinct());

			return bmp;
		}

		public void GeneratePreviewGrid(ProgressManager progressManager)
		{
			Stopwatch stopwatch = new Stopwatch();
			stopwatch.Start();

			GeneratePreviewPixelGrid(MAX_PREVIEW_DIMENSION, progressManager);
			
			progressManager.Log(stopwatch, "Generated preview");
		}

		private unsafe void GeneratePreviewPixelGrid(ushort maxPreviewDimension, ProgressManager progressManager)
		{
			float fillVal = -1.0f;
			Grid<float> grid = new Grid<float>(Extent, maxPreviewDimension, fillVal, true);
			Grid<uint> quantizedGrid = new Grid<uint>(grid.SizeX, grid.SizeY, Extent, true);

			double pixelsOverRangeX = (double)grid.SizeX / QuantizedExtent.RangeX;
			double pixelsOverRangeY = (double)grid.SizeY / QuantizedExtent.RangeY;

			uint minX = QuantizedExtent.MinX;
			uint minY = QuantizedExtent.MinY;

			byte[] inputBuffer = new byte[TileSet.Density.MaxTileCount * PointSizeBytes];

			fixed (byte* inputBufferPtr = inputBuffer)
			{
				foreach (PointCloudTileSourceEnumeratorChunk chunk in GetTileEnumerator(inputBuffer))
				{
					byte* pb = inputBufferPtr;
					byte* pbEnd = inputBufferPtr + chunk.Tile.StorageSize;
					while (pb < pbEnd)
					{
						UQuantizedPoint3D* p = (UQuantizedPoint3D*)(pb);
						pb += PointSizeBytes;

						int pixelX = (int)(((*p).X - minX) * pixelsOverRangeX);
						int pixelY = (int)(((*p).Y - minY) * pixelsOverRangeY);

						if ((*p).Z > quantizedGrid.Data[pixelX, pixelY])
							quantizedGrid.Data[pixelX, pixelY] = (*p).Z;
					}

					if (!progressManager.Update(chunk))
						break;
				}
			}

			quantizedGrid.CorrectMaxOverflow();
			quantizedGrid.CopyToUnquantized(grid, Quantization, Extent);

			
			// test
			//CachedColorRamp cachedRamp = new CachedColorRamp(ColorRamp.PredefinedColorRamps.Elevation1, QuantizedExtent.MinZ, QuantizedExtent.MaxZ, StatisticsZ.ConvertToQuantized(Quantization as UQuantization3D), true, 1000);

			m_pixelGrid = grid;
		}

		private unsafe BitmapSource CreateBitmapSource(Grid<float> grid, double rangeZ, bool useStdDevStretch, IColorHandler colorHandler)
		{
			ColorRamp ramp = colorHandler as ColorRamp;
			ColorMapDistinct map = colorHandler as ColorMapDistinct;

			WriteableBitmap bmp = new WriteableBitmap(grid.SizeX, grid.SizeY, 96, 96, System.Windows.Media.PixelFormats.Bgra32, null);
			bmp.Lock();
			int pBackBuffer = (int)bmp.BackBuffer;
			int* p = (int*)pBackBuffer;

			if (map != null)
			{
				CreateColorBufferMap(grid, p, map);
			}
			else
			{
				if (ramp == null)
					ramp = ColorRamp.PredefinedColorRamps.Grayscale;

				if (useStdDevStretch)
					CreateColorBufferStdDev(grid, p, ramp);
				else
					CreateColorBufferFull(grid, p, rangeZ, ramp);
			}

			bmp.AddDirtyRect(new System.Windows.Int32Rect(0, 0, bmp.PixelWidth, bmp.PixelHeight));
			bmp.Unlock();
			bmp.Freeze();

			return bmp;
		}

		#region Color Buffer Methods

		private unsafe void CreateColorBufferStdDev(Grid<float> grid, int* p, ColorRamp ramp)
		{
			float devFromMean = (float)(2 * StatisticsZ.StdDev);
			float stdDevOffset = (float)(StatisticsZ.Mean - Extent.MinZ - devFromMean);
			if (stdDevOffset < 0) stdDevOffset = 0;
			float rangeZ = 2 * devFromMean;

			for (int r = 0; r < grid.SizeY; r++)
			{
				for (int c = 0; c < grid.SizeX; c++)
				{
					// flip y-axis
					float z = grid.Data[c, grid.SizeY - r - 1];

					Color color = Color.Transparent;

					if (z != grid.FillVal)
					{
						z -= stdDevOffset;

						if (z < 0) z = 0; else if (z > rangeZ) z = rangeZ;
						double ratio = z / rangeZ;

						color = ramp.GetColor(ratio);
					}

					(*p) = color.ToArgb();
					++p;
				}
			}
		}

		private unsafe void CreateColorBufferFull(Grid<float> grid, int* p, double rangeZ, ColorRamp ramp)
		{
			for (int r = 0; r < grid.SizeY; r++)
			{
				for (int c = 0; c < grid.SizeX; c++)
				{
					// flip y-axis
					float z = grid.Data[c, grid.SizeY - r - 1];

					Color color = Color.Transparent;

					if (z != grid.FillVal)
					{
						double ratio = z / rangeZ;

						if (ratio < 0) ratio = 0.0f; else if (ratio > 1) ratio = 1.0f;

						color = ramp.GetColor(ratio);
					}

					(*p) = color.ToArgb();
					++p;
				}
			}
		}

		private unsafe void CreateColorBufferMap(Grid<float> grid, int* p, ColorMapDistinct map)
		{
			for (int r = 0; r < grid.SizeY; r++)
			{
				for (int c = 0; c < grid.SizeX; c++)
				{
					// flip y-axis
					float z = grid.Data[c, grid.SizeY - r - 1];

					Color color = Color.Transparent;

					if (z != grid.FillVal)
						color = map.GetColor((uint)z);

					(*p) = color.ToArgb();
					++p;
				}
			}
		}

		#endregion

		public PointCloudTileSourceEnumerator GetTileEnumerator(byte[] buffer)
		{
			return new PointCloudTileSourceEnumerator(this, buffer);
		}
	}
}
