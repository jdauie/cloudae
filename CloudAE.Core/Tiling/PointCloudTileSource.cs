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
using Jacere.Core;
using Jacere.Core.Geometry;
using Jacere.Data.PointCloud;

namespace CloudAE.Core
{
#warning TODO: remove dependence on BinarySource to simplify conversion to LAS 1.4
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
		private readonly QuantizedStatistics m_statisticsQuantizedZ;
		
		private SQuantizedExtent3D m_quantizedExtent;

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

		public QuantizedStatistics StatisticsQuantizedZ
		{
			get { return m_statisticsQuantizedZ; }
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

		public SQuantizedExtent3D QuantizedExtent
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

		public PointCloudTileSource(LASFile file, PointCloudTileSet tileSet, SQuantization3D quantization, short pointSizeBytes, Statistics zStats)
			: this(file, tileSet, quantization, 0, pointSizeBytes, zStats)
		{
		}

		public PointCloudTileSource(LASFile file, PointCloudTileSet tileSet, SQuantization3D quantization, long pointDataOffset, short pointSizeBytes, Statistics zStats)
			: base(file, tileSet.PointCount, tileSet.Extent, quantization, pointDataOffset, pointSizeBytes)
		{
			m_tileSet = tileSet;
			m_tileSet.TileSource = this;

			m_statisticsZ = zStats;
			m_statisticsQuantizedZ = zStats.ConvertToQuantized(Quantization);
#warning this should be stored in the tileset, rather than converted
			QuantizedExtent = Quantization.Convert(Extent);

			if (pointDataOffset == 0)
			{
				IsDirty = true;
				WriteHeader();
			}
		}

		public static string GetTileSourcePath(string path)
		{
			// mark with some low-order bytes of the file size
			var fileInfo = new FileInfo(path);
			string fileName = String.Format("{0}.{1}.{2}", fileInfo.Name, BitConverter.GetBytes(fileInfo.Length).ToBase64SafeString(0, 3), PointCloudTileSource.FILE_EXTENSION);
			string tilePath = Path.Combine(Cache.APP_CACHE_DIR, fileName);
			return tilePath;
		}

		public static PointCloudTileSource Open(LASFile file)
		{
			long pointDataOffset;
			short pointSizeBytes;

			SQuantization3D quantization;
			Statistics zStats;
			PointCloudTileSet tileSet;

			using (var reader = new BinaryReader(File.OpenRead(file.FilePath)))
			{
				if (Encoding.ASCII.GetString(reader.ReadBytes(FILE_IDENTIFIER.Length)) != FILE_IDENTIFIER)
					throw new OpenFailedException(file, "File identifier does not match.");

				int versionMajor = reader.ReadInt32();
				int versionMinor = reader.ReadInt32();

				if (versionMajor != FILE_VERSION_MAJOR || versionMinor != FILE_VERSION_MINOR)
					throw new OpenFailedException(file, "File version does not match.");

				pointDataOffset = reader.ReadInt64();
				pointSizeBytes = reader.ReadInt16();

				quantization = reader.ReadSQuantization3D();
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

		public void LoadTile(PointCloudTile tile, byte[] inputBuffer)
		{
			if (tile == null)
				throw new ArgumentNullException("tile");

			Open();

			tile.ReadTile(m_inputStream, inputBuffer, 0);
		}

		public void LoadTile(PointCloudTile tile, byte[] inputBuffer, int index)
		{
			if (tile == null)
				throw new ArgumentNullException("tile");

			Open();

			tile.ReadTile(m_inputStream, inputBuffer, index);
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

		public KeyValuePair<Grid<int>, Grid<float>> GenerateGrid(PointCloudTile template, ushort maxDimension)
		{
			Extent3D extent = template.Extent;

			float fillVal = (float)extent.MinZ - 1;
			var grid = Grid<float>.CreateBuffered(extent, 2, maxDimension, fillVal);
			var quantizedGrid = grid.Copy<int>();

			return new KeyValuePair<Grid<int>, Grid<float>>(quantizedGrid, grid);
		}

		public unsafe void LoadTileGrid(PointCloudTile tile, BufferInstance inputBuffer, Grid<float> grid, Grid<int> quantizedGrid)
		{
			Open();

			var quantizedExtent = tile.QuantizedExtent;

			double cellSizeX = (double)quantizedExtent.RangeX / grid.SizeX;
			double cellSizeY = (double)quantizedExtent.RangeY / grid.SizeY;

#warning Why did I do FillVal it this way?
			//grid.FillVal = -1.0f;
			grid.Reset();
			quantizedGrid.Reset();

			byte* inputBufferPtr = inputBuffer.DataPtr;
			
			int bytesRead = tile.ReadTile(m_inputStream, inputBuffer.Data);

			byte* pb = inputBufferPtr;
			byte* pbEnd = inputBufferPtr + tile.StorageSize;
			while (pb < pbEnd)
			{
				var p = (SQuantizedPoint3D*)pb;
				pb += PointSizeBytes;

				var pixelX = (int)(((*p).X - quantizedExtent.MinX) / cellSizeX);
				var pixelY = (int)(((*p).Y - quantizedExtent.MinY) / cellSizeY);

				// max val for now, apparently
				if ((*p).Z > quantizedGrid.Data[pixelY, pixelX])
					quantizedGrid.Data[pixelY, pixelX] = (*p).Z;
			}

			quantizedGrid.CorrectMaxOverflow();
			quantizedGrid.CopyToUnquantized(grid, Quantization, Extent);
		}

		private unsafe void TileOperationAction(IPointDataTileChunk chunk)
		{
            //ushort gridSize = (ushort)Math.Sqrt(chunk.Tile.PointCount);

            //var grid = new Grid<List<int>>(gridSize, gridSize, null, true);

            //UQuantizedExtent3D quantizedExtent = chunk.Tile.QuantizedExtent;

            //double cellSizeX = (double)quantizedExtent.RangeX / gridSize;
            //double cellSizeY = (double)quantizedExtent.RangeY / gridSize;

            //int i = 0;
            //byte* pb = chunk.PointDataPtr;
            //while (pb < chunk.PointDataEndPtr)
            //{
            //    var p = (SQuantizedPoint3D*)pb;

            //    int pixelX = (int)(((*p).X - quantizedExtent.MinX) / cellSizeX);
            //    int pixelY = (int)(((*p).Y - quantizedExtent.MinY) / cellSizeY);

            //    if (grid.Data[pixelX, pixelY] == null)
            //        grid.Data[pixelX, pixelY] = new List<int>();

            //    grid.Data[pixelX, pixelY].Add(i);

            //    pb += chunk.PointSizeBytes;
            //    ++i;
            //}

            //uint maxRegionCount = 0;

            //int windowSize = 10;
            //int windowRadius = windowSize / 2;
            //for (int x = 0; x < grid.SizeX; x++)
            //{
            //    for (int y = 0; y < grid.SizeY; y++)
            //    {
            //        var pointIndices = grid.Data[x, y];
            //        if (pointIndices != null)
            //        {
            //            uint regionCount = 0;
            //            for (int x1 = x - windowRadius; x1 < x + windowRadius; x1++)
            //            {
            //                for (int y1 = y - windowRadius; y1 < y + windowRadius; y1++)
            //                {
            //                    var pointIndices1 = grid.Data[x, y];
            //                    if(pointIndices1 != null)
            //                        regionCount += (uint)pointIndices1.Count;
            //                }
            //            }

            //            maxRegionCount = Math.Max(maxRegionCount, regionCount);

            //            for (int index = 0; index < pointIndices.Count; index++)
            //            {
            //                UQuantizedPoint3D* p = (UQuantizedPoint3D*)(chunk.PointDataPtr + chunk.PointSizeBytes * pointIndices[index]);
            //                (*p).Z = regionCount;
            //            }
            //        }
            //    }
            //}

            //pb = chunk.PointDataPtr;
            //while (pb < chunk.PointDataEndPtr)
            //{
            //    UQuantizedPoint3D* p = (UQuantizedPoint3D*)pb;

            //    if ((*p).Z > maxRegionCount)
            //        (*p).Z = maxRegionCount;

            //    (*p).Z = (*p).Z * quantizedExtent.RangeZ / maxRegionCount + quantizedExtent.MinZ;

            //    pb += chunk.PointSizeBytes;
            //}
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

		private BitmapSource GeneratePreviewImage(Grid<int> grid, ColorRamp ramp, bool useStdDevStretch, int quality)
		{
			BitmapSource bmp = CreateBitmapSource(grid, QuantizedExtent, StatisticsQuantizedZ, useStdDevStretch, ramp, quality);
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
						var cu = new Point3D(x * xMultiplier, y * yMultiplier, grid.Data[x, y]);
						var tp = new Point3D(x * xMultiplier, (y - 1) * yMultiplier, grid.Data[x, y - 1]);
						var lf = new Point3D((x - 1) * xMultiplier, y * yMultiplier, grid.Data[x - 1, y]);
						var tl = new Point3D((x - 1) * xMultiplier, (y - 1) * yMultiplier, grid.Data[x - 1, y - 1]);

						var plane0 = new Plane(cu, tp, tl, true);
						var plane1 = new Plane(cu, lf, tl, true);
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

		private void GeneratePreviewPixelGrid(ushort maxPreviewDimension, ProgressManager progressManager)
		{
			var gridSet = new GridQuantizedSet(this, maxPreviewDimension, -1.0f);
			var group = new ChunkProcessSet(gridSet);

			using (var process = progressManager.StartProcess("GeneratePreviewPixelGrid"))
			{
				group.Process(GetTileEnumerator(process));
			}

			m_pixelGridSet = gridSet;
		}

		private static unsafe BitmapSource CreateBitmapSource(Grid<int> grid, SQuantizedExtent3D extent, QuantizedStatistics statistics, bool useStdDevStretch, IColorHandler colorHandler, int quality)
		{
			var ramp = colorHandler as ColorRamp;
			var map = colorHandler as ColorMapDistinct;

			var bmp = new WriteableBitmap(grid.SizeX, grid.SizeY, 96, 96, System.Windows.Media.PixelFormats.Bgra32, null);
			bmp.Lock();
			var pBackBuffer = bmp.BackBuffer;
			var p = (int*)pBackBuffer;

			if (map != null)
			{
				CreateColorBufferMap(grid, p, map);
			}
			else
			{
				if (ramp == null)
					ramp = ColorRamp.PredefinedColorRamps.Grayscale;

				var qualityRatio = (float)quality / 100;
				var rampSize = (int)(qualityRatio * 300);

				StretchBase stretch = null;
				if(useStdDevStretch)
					stretch = new StdDevStretch(extent.MinZ, extent.MaxZ, statistics, 2);
				else
					stretch = new MinMaxStretch(extent.MinZ, extent.MaxZ);

				var cachedRamp = ramp.CreateCachedRamp(stretch, rampSize);

				CreateColorBufferMap(grid, p, cachedRamp);
			}

			bmp.AddDirtyRect(new System.Windows.Int32Rect(0, 0, bmp.PixelWidth, bmp.PixelHeight));
			bmp.Unlock();
			bmp.Freeze();

			return bmp;
		}

		#region Color Buffer Methods

		private static unsafe void CreateColorBufferMap(Grid<int> grid, int* p, IQuantizedColorMap cachedMap)
		{
			var transparent = Color.Transparent.ToArgb();

			fixed (int* gridPtr = grid.Data)
			{
				for (var r = 0; r < grid.SizeY; r++)
				{
					// flip y-axis
					int rr = grid.SizeY - r - 1;
					int* g = gridPtr + rr * grid.Data.GetLength(1);
					for (var c = 0; c < grid.SizeX; c++)
					{
						if (*g != grid.FillVal)
							(*p) = cachedMap.GetColor(*g);
						else
							(*p) = transparent;

						++p;
						++g;
					}
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
