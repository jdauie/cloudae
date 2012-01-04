using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Drawing.Imaging;
using System.Drawing;
using System.Diagnostics;
using System.Windows.Media.Imaging;

using CloudAE.Core.Geometry;
using CloudAE.Core.Compression;
using CloudAE.Core.Delaunay;
using CloudAE.Core.DelaunayIncremental;

namespace CloudAE.Core
{
	public class PointCloudTileSource : PointCloudBinarySource, IEnumerable<PointCloudTile>, ISerializeBinary
	{
		private const int MAX_PREVIEW_DIMENSION = 1000;

		// change this to TPBF?
		private const string FILE_IDENTIFIER = "TPBF";
		private const int FILE_VERSION_MAJOR = 1;
		private const int FILE_VERSION_MINOR = 7;

		public readonly PointCloudTileSet TileSet;
		public readonly Statistics StatisticsZ;
		
		private UQuantizedExtent3D m_quantizedExtent;

		private FileStream m_inputStream;

		private BitmapSource m_preview;
		private Grid<float> m_pixelGrid;

		public BitmapSource Preview
		{
			get { return m_preview; }
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
		
		public PointCloudTileSource(string file, PointCloudTileSet tileSet, Quantization3D quantization, Statistics zStats, CompressionMethod compression)
			: this(file, tileSet, quantization, 0, zStats, compression)
		{
		}

		public PointCloudTileSource(string file, PointCloudTileSet tileSet, Quantization3D quantization, long pointDataOffset, Statistics zStats, CompressionMethod compression)
			: base(file, tileSet.PointCount, tileSet.Extent, quantization, pointDataOffset, BufferManager.QUANTIZED_POINT_SIZE_BYTES, compression)
		{
			TileSet = new PointCloudTileSet(tileSet, this);
			StatisticsZ = zStats;
			QuantizedExtent = (UQuantizedExtent3D)Quantization.Convert(Extent);

			if (pointDataOffset == 0)
			{
				WriteHeader();
			}
		}

		public static PointCloudTileSource Open(string file)
		{
			long pointDataOffset;
			CompressionMethod compression;

			UQuantization3D quantization;
			Statistics zStats;
			PointCloudTileSet tileSet;

			using (BinaryReader reader = new BinaryReader(File.OpenRead(file)))
			{
				if (ASCIIEncoding.ASCII.GetString(reader.ReadBytes(FILE_IDENTIFIER.Length)) != FILE_IDENTIFIER)
					throw new Exception("File identifier does not match.");

				reader.ReadInt32(); // major version
				reader.ReadInt32(); // minor version

				pointDataOffset = reader.ReadInt64();
				compression = (CompressionMethod)reader.ReadInt32();

				quantization = reader.ReadUQuantization3D();
				zStats = reader.ReadStatistics();
				tileSet = reader.ReadTileSet();
			}

			PointCloudTileSource source = new PointCloudTileSource(file, tileSet, quantization, pointDataOffset, zStats, compression);

			return source;
		}

		public void Serialize(BinaryWriter writer)
		{
			writer.Write(ASCIIEncoding.ASCII.GetBytes(FILE_IDENTIFIER));
			writer.Write(FILE_VERSION_MAJOR);
			writer.Write(FILE_VERSION_MINOR);
			writer.Write(PointDataOffset);
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

				Console.WriteLine("Allocated {1}MB in {0}ms", stopwatch.ElapsedMilliseconds, outputLength / (1 << 20));
			}
		}

		public unsafe void LoadTile(PointCloudTile tile, byte[] inputBuffer)
		{
			if (tile.PointCount == 0)
				return;

			Open();

			int bytesRead = tile.ReadTile(m_inputStream, inputBuffer);

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
			//float pixelsOverRangeX = (float)grid.SizeX / quantizedExtent.RangeX;
			//float pixelsOverRangeY = (float)grid.SizeY / quantizedExtent.RangeY;
			// approximate, but probably good enough
			uint cellSizeX = quantizedExtent.RangeX / grid.SizeX;
			uint cellSizeY = quantizedExtent.RangeY / grid.SizeY;

			grid.FillVal = (float)tile.Extent.MinZ - 1;
			grid.Reset();
			quantizedGrid.Reset();

			fixed (byte* inputBufferPtr = inputBuffer)
			{
				UQuantizedPoint3D* p = (UQuantizedPoint3D*)inputBufferPtr;

				int bytesRead = tile.ReadTile(m_inputStream, inputBuffer);

				for (int i = 0; i < tile.PointCount; i++)
				{
					//int pixelX = (int)((p[i].X - quantizedExtent.MinX) * pixelsOverRangeX);
					//int pixelY = (int)((p[i].Y - quantizedExtent.MinY) * pixelsOverRangeY);

					uint pixelX = (p[i].X - quantizedExtent.MinX) / cellSizeX;
					uint pixelY = (p[i].Y - quantizedExtent.MinY) / cellSizeY;

					if (p[i].Z > quantizedGrid.Data[pixelX, pixelY])
						quantizedGrid.Data[pixelX, pixelY] = p[i].Z;
				}
			}

			// correct edge overflows
			for (int x = 0; x <= grid.SizeX; x++)
				quantizedGrid.Data[x, grid.SizeY - 1] = Math.Max(quantizedGrid.Data[x, grid.SizeY], quantizedGrid.Data[x, grid.SizeY - 1]);
			for (int y = 0; y < grid.SizeY; y++)
				quantizedGrid.Data[grid.SizeX - 1, y] = Math.Max(quantizedGrid.Data[grid.SizeX, y], quantizedGrid.Data[grid.SizeX - 1, y]);

			// transform quantized values
			for (int x = 0; x < grid.SizeX; x++)
				for (int y = 0; y < grid.SizeY; y++)
					if (quantizedGrid.Data[x, y] > 0) // ">" zero is not quite what I want here
						grid.Data[x, y] = (float)(quantizedGrid.Data[x, y] * Quantization.ScaleFactorZ + Quantization.OffsetZ);

			//// TESTING
			//float[] values = grid.Data.Cast<float>().Where(v => v != grid.FillVal).ToArray();
			//Array.Sort<float>(values);

			//// histogram analysis?
			//float groundVal = values[0];
			//for (int i = 1; i < values.Length; i++)
			//{
			//    // while the gap is small, keep climbing
			//    float diff = values[i] - groundVal;

			//    if (diff < 1.0f)
			//        groundVal = values[i];
			//}

			//for (int x = 0; x < grid.SizeX; x++)
			//{
			//    for (int y = 0; y < grid.SizeY; y++)
			//    {
			//        if (grid.Data[x, y] > groundVal)
			//            grid.Data[x, y] = groundVal;
			//    }
			//}
		}

		public System.Windows.Media.Media3D.MeshGeometry3D GenerateMesh(Grid<float> grid, Extent3D distributionExtent)
		{
			// subtract midpoint to center around (0,0,0)
			Extent3D centeringExtent = Extent;

			System.Windows.Media.Media3D.Point3DCollection positions = new System.Windows.Media.Media3D.Point3DCollection(grid.CellCount);
			System.Windows.Media.Int32Collection indices = new System.Windows.Media.Int32Collection(2 * (grid.SizeX - 1) * (grid.SizeY - 1));
			
			float fillVal = grid.FillVal;

			for (int x = 0; x < grid.SizeX; x++)
			{
				for (int y = 0; y < grid.SizeY; y++)
				{
					double value = grid.Data[x, y] + centeringExtent.MinZ - centeringExtent.MidpointZ;

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

						//System.Windows.Media.Media3D.Point3D cuPoint = geometry.Positions[currentPosition];
						//System.Windows.Media.Media3D.Point3D lfPoint = geometry.Positions[leftPosition];
						//System.Windows.Media.Media3D.Point3D tpPoint = geometry.Positions[topPosition];
						//System.Windows.Media.Media3D.Point3D tlPoint = geometry.Positions[topleftPosition];

						if (grid.Data[x - 1, y] != fillVal && grid.Data[x, y - 1] != fillVal)
						{
							if (grid.Data[x, y] != fillVal)
							{
								indices.Add(leftPosition);
								indices.Add(topPosition);
								indices.Add(currentPosition);
							}

							if (grid.Data[x - 1, y - 1] != fillVal)
							{
								indices.Add(topleftPosition);
								indices.Add(topPosition);
								indices.Add(leftPosition);
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

		//public unsafe System.Windows.Media.Media3D.MeshGeometry3D LoadTileMesh(PointCloudTile tile, byte[] inputBuffer)
		//{
		//    return LoadTileGridMesh(tile, inputBuffer);
		//}

		//public unsafe System.Windows.Media.Media3D.MeshGeometry3D LoadTileGridMesh(PointCloudTile tile, byte[] inputBuffer)
		//{
		//    Grid<float> grid = LoadTileGrid(tile, inputBuffer, 100);
		//    System.Windows.Media.Media3D.MeshGeometry3D mesh = GenerateMesh(grid, tile.Extent);

		//    return mesh;
		//}

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
			if (maxIndividualBufferSize > BufferManager.BUFFER_SIZE_BYTES)
				throw new Exception("tile size was not anticipated to be larger than buffer size");

			Stopwatch stopwatch = new Stopwatch();
			stopwatch.Start();

			string outputTempFile = ProcessingSet.GetTemporaryCompressedTileSourceName(FilePath);

			double compressionRatio = 0.0;
			double pretendCompressionRatio = 0.0;

			long compressedCount = 0;

			byte[] inputBuffer = BufferManager.AcquireBuffer();
			byte[] outputBuffer = BufferManager.AcquireBuffer();

			//long[] byteProbabilityCounts = new long[256];

			using (FileStream inputStream = new FileStream(FilePath, FileMode.Open, FileAccess.Read, FileShare.None, BufferManager.BUFFER_SIZE_BYTES, FileOptions.SequentialScan))
			using (FileStream outputStream = new FileStream(outputTempFile, FileMode.Create, FileAccess.Write, FileShare.None, BufferManager.BUFFER_SIZE_BYTES, FileOptions.SequentialScan))
			{
				long inputLength = inputStream.Length;
				outputStream.SetLength(inputLength);

				fixed (byte* inputBufferPtr = inputBuffer)
				{
					UQuantizedPoint3D* p = (UQuantizedPoint3D*)inputBufferPtr;

					foreach (PointCloudTile tile in this)
					{
						int bytesRead = tile.ReadTile(inputStream, inputBuffer);

						UQuantizedExtent3D qExtent = tile.QuantizedExtent;
						uint maxDeltaZ = SortAndDeltaEncode(p, tile.PointCount, qExtent);

						// check the byte frequencies
						//for (int bIndex = 0; bIndex < bytesRead; bIndex++)
						//{
						//    ++byteProbabilityCounts[*(inputBufferPtr + bIndex)];
						//}

						// check the bit-compaction
						//int zBits = (int)Math.Ceiling(Math.Log(maxZ, 2));
						//int xBits = (int)Math.Ceiling(Math.Log(qExtent.RangeX, 2));
						//int yBits = (int)Math.Ceiling(Math.Log(qExtent.RangeY, 2));

						//pretendCompressionRatio += (double)tile.PointCount * (xBits + yBits + zBits) / (PointSizeBytes * 8);


						int compressedSize = bytesRead;

						switch (compressionMethod)
						{
							case CompressionMethod.QuickLZ:
								compressedSize = QuickLZ.compress(inputBuffer, bytesRead, outputBuffer, 1);
								break;

							case CompressionMethod.DotNetZip:
								compressedSize = SevenZipSharp.Compress(inputBuffer, bytesRead, outputBuffer);
								break;

							case CompressionMethod.SevenZipSharp:
								compressedSize = DotNetZip.Compress(inputBuffer, bytesRead, outputBuffer);
								break;

							case CompressionMethod.Default:
								break;
						}

						byte[] bufferToWrite = outputBuffer;
						int bytesToWrite = compressedSize;

						if (compressedSize >= bytesRead)
						{
							bytesToWrite = bytesRead;
							bufferToWrite = inputBuffer;
						}

						// make new tile object with compressed offset/size
						TileSet.Tiles[tile.Col, tile.Row] = new PointCloudTile(tile, compressedCount, bytesToWrite);

						// write out buffer (same file or different file?)
						outputStream.Write(bufferToWrite, 0, bytesToWrite);

						compressedCount += bytesToWrite;

						if (!progressManager.Update((float)tile.Index / TileSet.TileCount))
							break;
					}
				}

				outputStream.SetLength(outputStream.Position);

				compressionRatio = (double)compressedCount / inputLength;
				pretendCompressionRatio /= Count;
			}

			BufferManager.ReleaseBuffer(outputBuffer);
			BufferManager.ReleaseBuffer(inputBuffer);

			byte[] byteProbabilityValues = new byte[256];
			for (int i = 0; i < 256; i++)
				byteProbabilityValues[i] = (byte)i;

			//Array.Sort<long, byte>(byteProbabilityCounts, byteProbabilityValues);

			stopwatch.Stop();
			progressManager.Log(stopwatch, "Compressed tiles ({0:f}) ({1:f})", compressionRatio, pretendCompressionRatio);

			File.Delete(FilePath);
			File.Move(outputTempFile, FilePath);

			PointCloudTileSource newTileSource = new PointCloudTileSource(FilePath, TileSet, Quantization, StatisticsZ, compressionMethod);

			return newTileSource;
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
				} while (a[j].Z > a[p].Z);
				do
				{
					++i;
				} while (a[i].Z < a[p].Z);
				if (i < j)
				{
					tmp = a[i];
					a[i] = a[j];
					a[j] = tmp;
				}
				else return j;
			}
		}

		private unsafe uint SortAndDeltaEncode(UQuantizedPoint3D* p, int count, UQuantizedExtent3D quantizedExtent)
		{
			QuickSort(p, 0, count - 1);

			// delta encoding on single component
			// but offset the others
			uint maxZ = 0;
			uint lastZ = quantizedExtent.MinZ;
			for (int i = 0; i < count; i++)
			{
				uint diff = p[i].Z - lastZ;
				lastZ = p[i].Z;
				p[i].Z = diff;
				if (diff > maxZ) maxZ = diff;

				p[i].X -= quantizedExtent.MinX;
				p[i].Y -= quantizedExtent.MinY;
			}

			return maxZ;
		}

		public unsafe BitmapSource GeneratePreview(ProgressManager progressManager)
		{
			return GeneratePreview(MAX_PREVIEW_DIMENSION, progressManager);
		}

		public unsafe BitmapSource GeneratePreview(ushort maxPreviewDimension, ProgressManager progressManager)
		{
			Stopwatch stopwatch = new Stopwatch();
			stopwatch.Start();

			Grid<float> grid = GeneratePreviewPixelGrid(maxPreviewDimension, progressManager);
			Bitmap bmp = CreateBitmap(grid, Extent.RangeZ, true, ColorRamp.PredefinedColorRamps.Elevation1);
			//Bitmap bmp = CreateSegmentationBitmap(grid);
			//Bitmap bmp = CreatePlaneFittingBitmap(grid);

			progressManager.Log(stopwatch, "Generated preview");

			m_preview = Util.LoadBitmap(bmp);
			m_preview.Freeze();

			return m_preview;
		}

		public unsafe BitmapSource GeneratePreviewImage(Grid<float> grid)
		{
			Bitmap bmp = CreateBitmap(grid, Extent.RangeZ, true, ColorRamp.PredefinedColorRamps.Elevation1);
			//Bitmap bmp = CreateSegmentationBitmap(grid);
			//Bitmap bmp = CreatePlaneFittingBitmap(grid);

			BitmapSource bmpSource = Util.LoadBitmap(bmp);
			bmpSource.Freeze();

			return bmpSource;
		}

		private Bitmap CreatePlaneFittingBitmap(Grid<float> grid)
		{
			float[,] gridValues = new float[grid.SizeX, grid.SizeY];

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

			for (int x = 0; x < grid.SizeX; x++)
				for (int y = 0; y < grid.SizeY; y++)
					grid.Data[x, y] = gridValues[x, y];

			Bitmap bmp = CreateBitmap(grid, 1.0, false, null);

			return bmp;
		}

		private Bitmap CreateSegmentationBitmap(Grid<float> grid)
		{
			uint[,] gridClasses = new uint[grid.SizeX, grid.SizeY];

			uint currentClassIndex = Segmentation.ClassifyTile(grid, gridClasses, 1.00f, 1, 10, grid.FillVal, NeighborhoodType.FourNeighbors);

			for (int x = 0; x < grid.SizeX; x++)
				for (int y = 0; y < grid.SizeY; y++)
					grid.Data[x, y] = gridClasses[x, y];

			Bitmap bmp = CreateBitmap(grid, currentClassIndex, false, new ColorMapDistinct());

			return bmp;
		}

		public unsafe Grid<float> GeneratePreviewGrid(ushort maxPreviewDimension, ProgressManager progressManager)
		{
			Stopwatch stopwatch = new Stopwatch();
			stopwatch.Start();

			Grid<float> grid = GeneratePreviewPixelGrid(maxPreviewDimension, progressManager);
			
			progressManager.Log(stopwatch, "Generated preview grid");

			return grid;
		}

		public unsafe Grid<float> GeneratePreviewPixelGrid(ushort maxPreviewDimension, ProgressManager progressManager)
		{
			float fillVal = (float)Extent.MinZ - 1;
			Grid<float> grid = new Grid<float>(Extent, maxPreviewDimension, fillVal, true);
			Grid<uint> quantizedGrid = new Grid<uint>(grid.SizeX, grid.SizeY, Extent, true);

			double pixelsOverRangeX = (double)grid.SizeX / QuantizedExtent.RangeX;
			double pixelsOverRangeY = (double)grid.SizeY / QuantizedExtent.RangeY;

			uint minX = QuantizedExtent.MinX;
			uint minY = QuantizedExtent.MinY;

			using (FileStream inputStream = new FileStream(FilePath, FileMode.Open, FileAccess.Read, FileShare.None, BufferManager.BUFFER_SIZE_BYTES))
			{
				long inputLength = inputStream.Length;

				byte[] inputBuffer = new byte[TileSet.Density.MaxTileCount * PointSizeBytes];

				fixed (byte* inputBufferPtr = inputBuffer)
				{
					foreach (PointCloudTile tile in TileSet)
					{
						int bytesRead = tile.ReadTile(inputStream, inputBuffer);

						for (UQuantizedPoint3D* p = (UQuantizedPoint3D*)inputBufferPtr, end = p + tile.PointCount; p < end; ++p)
						{
							int pixelX = (int)(((*p).X - minX) * pixelsOverRangeX);
							int pixelY = (int)(((*p).Y - minY) * pixelsOverRangeY);

							if ((*p).Z > quantizedGrid.Data[pixelX, pixelY])
								quantizedGrid.Data[pixelX, pixelY] = (*p).Z;
						}

						if (!progressManager.Update((float)inputStream.Position / inputLength))
							break;
					}
				}
			}

			// correct edge overflows
			for (int x = 0; x <= grid.SizeX; x++)
				quantizedGrid.Data[x, grid.SizeY - 1] = Math.Max(quantizedGrid.Data[x, grid.SizeY], quantizedGrid.Data[x, grid.SizeY - 1]);
			for (int y = 0; y < grid.SizeY; y++)
				quantizedGrid.Data[grid.SizeX - 1, y] = Math.Max(quantizedGrid.Data[grid.SizeX, y], quantizedGrid.Data[grid.SizeX - 1, y]);

			// transform quantized values
			for (int x = 0; x < grid.SizeX; x++)
				for (int y = 0; y < grid.SizeY; y++)
					if (quantizedGrid.Data[x, y] > 0) // ">" zero is not quite what I want here
						grid.Data[x, y] = (float)(quantizedGrid.Data[x, y] * Quantization.ScaleFactorZ + Quantization.OffsetZ);

			m_pixelGrid = grid;

			return grid;
		}

		private unsafe Bitmap CreateBitmap(Grid<float> grid, double rangeZ, bool useStdDevStretch, IColorHandler colorHandler)
		{
			float stdDevOffset = 0;
			if (useStdDevStretch)
			{
				//Statistics stats = grid.Data.ToEnumerable<float>().ComputeStatistics(grid.FillVal);
				// this only makes sense if rangeZ == Extent.RangeZ
				float devFromMean = (float)(2 * StatisticsZ.StdDev);
				stdDevOffset = (float)StatisticsZ.Mean - devFromMean;
				rangeZ = 2 * devFromMean;
			}

			Bitmap bmp = new Bitmap(grid.SizeX, grid.SizeY, PixelFormat.Format32bppArgb);
			const int pixelSize = 4;

			BitmapData bmpWrite = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.WriteOnly, bmp.PixelFormat);

			ColorRamp ramp = colorHandler as ColorRamp;
			ColorMapDistinct map = colorHandler as ColorMapDistinct;

			for (int r = 0; r < grid.SizeY; r++)
			{
				byte* row = (byte*)bmpWrite.Scan0 + (r * bmpWrite.Stride);

				for (int c = 0; c < grid.SizeX; c++)
				{
					int* pixel = (int*)(row + (c * pixelSize));
					// flip y-axis
					float z = grid.Data[c, grid.SizeY - r - 1];

					Color color = Color.Transparent;

					if (z != grid.FillVal)
					{
						if (useStdDevStretch)
						{
							z -= stdDevOffset;
							if (z < 0) z = 0;
						}

						double ratio = Math.Min(z / rangeZ, 1.0);
						if (ramp != null)
						{
							color = ramp.GetColor(ratio);
						}
						else if (map != null)
						{
							color = map.GetColor((uint)z);
						}
						else
						{
							int colorZ = (int)(ratio * 255.0);
							color = Color.FromArgb(colorZ, colorZ, colorZ);
						}
					}

					pixel[0] = color.ToArgb();
				}
			}

			bmp.UnlockBits(bmpWrite);

			return bmp;
		}

		#region IEnumerable Members

		public IEnumerator<PointCloudTile> GetEnumerator()
		{
			return TileSet.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		#endregion
	}
}
