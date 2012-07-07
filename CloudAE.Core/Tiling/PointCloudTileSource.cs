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

		private IStreamReader m_inputStream;
		private bool m_isDirty;

		private GridQuantizedSet m_pixelGridSet;
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

		public long FileSize
		{
			get { return Count * PointSizeBytes + PointDataOffset; }
		}

		public int MaxTileBufferSize
		{
			get { return TileSet.Density.MaxTileCount * PointSizeBytes; }
		}

		#endregion

		public PointCloudTileSource(string file, PointCloudTileSet tileSet, Quantization3D quantization, short pointSizeBytes, Statistics zStats)
			: this(file, tileSet, quantization, 0, pointSizeBytes, zStats)
		{
		}

		public PointCloudTileSource(string file, PointCloudTileSet tileSet, Quantization3D quantization, long pointDataOffset, short pointSizeBytes, Statistics zStats)
			: base(file, tileSet.PointCount, tileSet.Extent, quantization, pointDataOffset, pointSizeBytes)
		{
			m_tileSet = new PointCloudTileSet(tileSet, this);
			m_statisticsZ = zStats;
#warning this should be stored in the tileset, rather than converted
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

			UQuantization3D quantization;
			Statistics zStats;
			PointCloudTileSet tileSet;

			using (var reader = new BinaryReader(File.OpenRead(file)))
			{
				if (Encoding.ASCII.GetString(reader.ReadBytes(FILE_IDENTIFIER.Length)) != FILE_IDENTIFIER)
					throw new OpenFailedException(file, "File identifier does not match.");

				int versionMajor = reader.ReadInt32();
				int versionMinor = reader.ReadInt32();

				if (versionMajor != FILE_VERSION_MAJOR || versionMinor != FILE_VERSION_MINOR)
					throw new OpenFailedException(file, "File version does not match.");

				pointDataOffset = reader.ReadInt64();
				pointSizeBytes = reader.ReadInt16();

				quantization = reader.ReadUQuantization3D();
				zStats = reader.ReadStatistics();
				tileSet = reader.ReadTileSet();
			}

			var source = new PointCloudTileSource(file, tileSet, quantization, pointDataOffset, pointSizeBytes, zStats);

			return source;
		}

		public void Serialize(BinaryWriter writer)
		{
			// assumes this function will be atomic, which is true for cancellation, 
			// but not for exceptions during the write operation
			string identifier = IsDirty ? FILE_IDENTIFIER_DIRTY : FILE_IDENTIFIER;

			writer.Write(Encoding.ASCII.GetBytes(identifier));
			writer.Write(FILE_VERSION_MAJOR);
			writer.Write(FILE_VERSION_MINOR);
			writer.Write(PointDataOffset);
			writer.Write(PointSizeBytes);
			writer.Write(Quantization);
			writer.Write(StatisticsZ);
			writer.Write(TileSet);
		}

		public void WriteHeader()
		{
			Close();

			using (var writer = new BinaryWriter(File.OpenWrite(FilePath)))
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
				m_inputStream = StreamManager.OpenReadStream(FilePath, PointDataOffset);
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
				using (var outputStream = new FileStream(FilePath, FileMode.OpenOrCreate, FileAccess.Write, FileShare.None, BufferManager.BUFFER_SIZE_BYTES, FileOptions.SequentialScan))
				{
					outputStream.SetLength(outputLength);

					if (!allowSparse)
					{
						outputStream.Seek(outputLength - 1, SeekOrigin.Begin);
						outputStream.WriteByte(1);
					}
				}

				stopwatch.Stop();

				if (!allowSparse)
					Context.WriteLine("Allocated {1} in {0}ms", stopwatch.ElapsedMilliseconds, outputLength.ToSize());
			}
		}

		public void LoadTile(PointCloudTile tile, byte[] inputBuffer)
		{
			if (tile.PointCount == 0)
				return;

			Open();

			tile.ReadTile(m_inputStream, inputBuffer, 0);
		}

		public void LoadTile(PointCloudTile tile, byte[] inputBuffer, int index)
		{
			if (tile.PointCount == 0)
				return;

			Open();

			tile.ReadTile(m_inputStream, inputBuffer, index);
		}

		public unsafe System.Windows.Media.Media3D.MeshGeometry3D LoadTilePointMesh(PointCloudTile tile, byte[] inputBuffer, double pointSize, int thinByFactor)
		{
			/*LoadTile(tile, inputBuffer);

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

			return geometry;*/

			return null;
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

		public unsafe void LoadTileGrid(PointCloudTile tile, BufferInstance inputBuffer, Grid<float> grid, Grid<uint> quantizedGrid)
		{
			Open();

			UQuantizedExtent2D quantizedExtent = tile.QuantizedExtent;

			double cellSizeX = (double)quantizedExtent.RangeX / grid.SizeX;
			double cellSizeY = (double)quantizedExtent.RangeY / grid.SizeY;

			grid.FillVal = -1.0f;
			grid.Reset();
			quantizedGrid.Reset();

			byte* inputBufferPtr = inputBuffer.DataPtr;
			
			int bytesRead = tile.ReadTile(m_inputStream, inputBuffer.Data);

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

			quantizedGrid.CorrectMaxOverflow();
			quantizedGrid.CopyToUnquantized(grid, Quantization, Extent);
		}

		private unsafe void TileOperationAction(IPointDataTileChunk chunk)
		{
			ushort gridSize = (ushort)Math.Sqrt(chunk.Tile.PointCount);

			var grid = new Grid<List<int>>(gridSize, gridSize, null, true);

			UQuantizedExtent3D quantizedExtent = chunk.Tile.QuantizedExtent;

			double cellSizeX = (double)quantizedExtent.RangeX / gridSize;
			double cellSizeY = (double)quantizedExtent.RangeY / gridSize;

			int i = 0;
			byte* pb = chunk.PointDataPtr;
			while (pb < chunk.PointDataEndPtr)
			{
				UQuantizedPoint3D* p = (UQuantizedPoint3D*)pb;

				int pixelX = (int)(((*p).X - quantizedExtent.MinX) / cellSizeX);
				int pixelY = (int)(((*p).Y - quantizedExtent.MinY) / cellSizeY);

				if (grid.Data[pixelX, pixelY] == null)
					grid.Data[pixelX, pixelY] = new List<int>();

				grid.Data[pixelX, pixelY].Add(i);

				pb += chunk.PointSizeBytes;
				++i;
			}

			uint maxRegionCount = 0;

			int windowSize = 10;
			int windowRadius = windowSize / 2;
			for (int x = 0; x < grid.SizeX; x++)
			{
				for (int y = 0; y < grid.SizeY; y++)
				{
					var pointIndices = grid.Data[x, y];
					if (pointIndices != null)
					{
						uint regionCount = 0;
						for (int x1 = x - windowRadius; x1 < x + windowRadius; x1++)
						{
							for (int y1 = y - windowRadius; y1 < y + windowRadius; y1++)
							{
								var pointIndices1 = grid.Data[x, y];
								if(pointIndices1 != null)
									regionCount += (uint)pointIndices1.Count;
							}
						}

						maxRegionCount = Math.Max(maxRegionCount, regionCount);

						for (int index = 0; index < pointIndices.Count; index++)
						{
							UQuantizedPoint3D* p = (UQuantizedPoint3D*)(chunk.PointDataPtr + chunk.PointSizeBytes * pointIndices[index]);
							(*p).Z = regionCount;
						}
					}
				}
			}

			pb = chunk.PointDataPtr;
			while (pb < chunk.PointDataEndPtr)
			{
				UQuantizedPoint3D* p = (UQuantizedPoint3D*)pb;

				if ((*p).Z > maxRegionCount)
					(*p).Z = maxRegionCount;

				(*p).Z = (*p).Z * quantizedExtent.RangeZ / maxRegionCount + quantizedExtent.MinZ;

				pb += chunk.PointSizeBytes;
			}
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
			double centerOfMassMinusMin = centerOfMass.Z - Extent.MinZ;

			var positions = new System.Windows.Media.Media3D.Point3DCollection(grid.CellCount);
			var indices = new System.Windows.Media.Int32Collection(2 * (grid.SizeX - 1) * (grid.SizeY - 1));
			
			float fillVal = grid.FillVal;

			for (int x = 0; x < grid.SizeX; x++)
			{
				for (int y = 0; y < grid.SizeY; y++)
				{
					double value = grid.Data[x, y] - centerOfMassMinusMin;

					double xCoord = ((double)x / grid.SizeX) * distributionExtent.RangeX + distributionExtent.MinX - distributionExtent.MidpointX;
					double yCoord = ((double)y / grid.SizeY) * distributionExtent.RangeY + distributionExtent.MinY - distributionExtent.MidpointY;

					xCoord += (distributionExtent.MidpointX - centeringExtent.MidpointX);
					yCoord += (distributionExtent.MidpointY - centeringExtent.MidpointY);

					var point = new System.Windows.Media.Media3D.Point3D(xCoord, yCoord, value);
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

			var normals = new System.Windows.Media.Media3D.Vector3DCollection(positions.Count);

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
					var normal = normals[i];
					normal.Normalize();

					// the fact that this is necessary means I am doing something wrong
					if (normal.Z < 0)
						normal.Negate();

					normals[i] = normal;
				}
			}

			var geometry = new System.Windows.Media.Media3D.MeshGeometry3D
			{
				Positions = positions, 
				TriangleIndices = indices, 
				Normals = normals
			};

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

		public BitmapSource GeneratePreviewImage(ColorRamp ramp, bool useStdDevStretch, int quality)
		{
			if (Preview == null || Preview.ColorHandler != ramp || Preview.UseStdDevStretch != useStdDevStretch || Preview.Quality != quality)
			{
				BitmapSource source = GeneratePreviewImage(m_pixelGridSet.GridQuantized, ramp, useStdDevStretch, quality);
				Preview = new PreviewImage(source, ramp, useStdDevStretch, quality);
			}
			return Preview.Image;
		}

		private BitmapSource GeneratePreviewImage(Grid<uint> grid, ColorRamp ramp, bool useStdDevStretch, int quality)
		{
			BitmapSource bmp = CreateBitmapSource(grid, QuantizedExtent, StatisticsZ.ConvertToQuantized(Quantization as UQuantization3D), useStdDevStretch, ramp, quality);
			//BitmapSource bmp = CreateSegmentationBitmap(grid);
			//BitmapSource bmp = CreatePlaneFittingBitmap(grid);

			return bmp;
		}

		private BitmapSource CreatePlaneFittingBitmap(Grid<uint> grid)
		{
			//float[,] gridValues = new float[grid.SizeX, grid.SizeY];

			//ComputeGridSlopeSurfaceComponent(grid, gridValues);

			//for (int x = 0; x < grid.SizeX; x++)
			//    for (int y = 0; y < grid.SizeY; y++)
			//        grid.Data[x, y] = gridValues[x, y];

			//BitmapSource bmp = CreateBitmapSource(grid, 1.0, false, null);

			//return bmp;

			return null;
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
			//uint[,] gridClasses = new uint[grid.SizeX, grid.SizeY];

			//uint currentClassIndex = Segmentation.ClassifyTile(grid, gridClasses, 1.00f, 1, 10, grid.FillVal, NeighborhoodType.FourNeighbors);

			//for (int x = 0; x < grid.SizeX; x++)
			//    for (int y = 0; y < grid.SizeY; y++)
			//        grid.Data[x, y] = gridClasses[x, y];

			//BitmapSource bmp = CreateBitmapSource(grid, currentClassIndex, false, new ColorMapDistinct());

			//return bmp;

			return null;
		}

		public void GeneratePreviewGrid(ProgressManager progressManager)
		{
			var stopwatch = new Stopwatch();
			stopwatch.Start();

			GeneratePreviewPixelGrid(MAX_PREVIEW_DIMENSION, progressManager);
			
			progressManager.Log(stopwatch, "Generated preview");
		}

		private unsafe void GeneratePreviewPixelGrid(ushort maxPreviewDimension, ProgressManager progressManager)
		{
			var gridSet = new GridQuantizedSet(this, maxPreviewDimension, -1.0f, true);

			using (var process = progressManager.StartProcess("GeneratePreviewPixelGrid"))
			{
				foreach (PointCloudTileSourceEnumeratorChunk chunk in GetTileEnumerator(process))
				{
					//TileOperationAction(chunk);

					gridSet.Process(chunk);
				}
			}

			gridSet.Populate();

			m_pixelGridSet = gridSet;
		}

		private unsafe BitmapSource CreateBitmapSource(Grid<uint> grid, UQuantizedExtent3D extent, QuantizedStatistics statistics, bool useStdDevStretch, IColorHandler colorHandler, int quality)
		{
			var ramp = colorHandler as ColorRamp;
			var map = colorHandler as ColorMapDistinct;

			var bmp = new WriteableBitmap(grid.SizeX, grid.SizeY, 96, 96, System.Windows.Media.PixelFormats.Bgra32, null);
			bmp.Lock();
			IntPtr pBackBuffer = bmp.BackBuffer;
			int* p = (int*)pBackBuffer;

			if (map != null)
			{
				CreateColorBufferMap(grid, p, map);
			}
			else
			{
				if (ramp == null)
					ramp = ColorRamp.PredefinedColorRamps.Grayscale;

				float qualityRatio = (float)quality / 100;
				int rampSize = (int)(qualityRatio * 300);
				
				var cachedRamp = ramp.CreateCachedRamp(extent.MinZ, extent.MaxZ, statistics, useStdDevStretch, rampSize);

				int transparent = Color.Transparent.ToArgb();

				for (int r = 0; r < grid.SizeY; r++)
				{
					int rr = grid.SizeY - r - 1;
					for (int c = 0; c < grid.SizeX; c++)
					{
						// flip y-axis
						uint z = grid.Data[c, rr];

						if (z != grid.FillVal)
							(*p) = cachedRamp.DestinationBins[z >> cachedRamp.SourceRightShift];
						else
							(*p) = transparent;

						++p;
					}
				}
			}

			bmp.AddDirtyRect(new System.Windows.Int32Rect(0, 0, bmp.PixelWidth, bmp.PixelHeight));
			bmp.Unlock();
			bmp.Freeze();

			return bmp;
		}

		#region Color Buffer Methods

		private unsafe void CreateColorBufferMap(Grid<uint> grid, int* p, ColorMapDistinct map)
		{
			for (int r = 0; r < grid.SizeY; r++)
			{
				for (int c = 0; c < grid.SizeX; c++)
				{
					// flip y-axis
					uint z = grid.Data[c, grid.SizeY - r - 1];

					Color color = Color.Transparent;

					if (z != grid.FillVal)
						color = map.GetColor(z);

					(*p) = color.ToArgb();
					++p;
				}
			}
		}

		#endregion

		public PointCloudTileSourceEnumerator GetTileEnumerator(ProgressManagerProcess process)
		{
			return new PointCloudTileSourceEnumerator(this, process);
		}
	}
}
