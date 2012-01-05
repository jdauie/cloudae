//using System;
//using System.Collections.Generic;
//using System.Text;

//namespace CloudAE.Core
//{
//    /// <summary>
//    /// Region growing segmentation algorithm.
//    /// </summary>
//    public class Segmentation
//    {
//        /// <summary>
//        /// Specifies the neighbor comparison method to use for surrounding pixels.
//        /// </summary>
//        /// <code>
//        /// -------------------
//        /// | TL  |  T  |  TR |
//        /// -------------------
//        /// |  L  |  C  |     |
//        /// -------------------
//        /// |     |     |     |
//        /// -------------------
//        /// </code>
//        public enum NeighborhoodType
//        {
//            /// <summary>Compares the current pixel to Top and Left only.</summary>
//            FourNeighbors,
//            /// <summary>Compares the current pixel to Top, Left, TopLeft, and TopRight.</summary>
//            EightNeighbors
//        };

//        /// <summary>
//        /// Specifies the output type of the segmentation raster.
//        /// </summary>
//        public enum OutputType
//        {
//            /// <summary>The output is the size of the segment (in pixels).</summary>
//            Size,
//            /// <summary>The output is the index of the segment.</summary>
//            Index
//        };

//        /// <summary>
//        /// Creates the output image.
//        /// </summary>
//        /// <param name="inImage">The template image.</param>
//        /// <returns></returns>
//        public static IMutableImage CreateOutputImage(IImage inImage)
//        {
//            return CreateOutputImage(inImage, null, null);
//        }

//        /// <summary>
//        /// Creates the output image.
//        /// </summary>
//        /// <param name="inImage">The template image.</param>
//        /// <param name="path">The path.</param>
//        /// <param name="fileName">Name of the file.</param>
//        /// <returns></returns>
//        public static IMutableImage CreateOutputImage(IImage inImage, string path, string fileName)
//        {
//            ParameterList imgCreationParams = CreateOutputImageParameters(inImage);

//            IMutableImage imageClone = null;

//            if (path != null && fileName != null)
//            {
//                DataSourceFolderConnection connection = new DataSourceFolderConnection(inImage.ImageFactory.ConnectionFactory, path);
//                imgCreationParams.GetParameter(ImageCreator.ConnectionParameterName).Value = connection;
//                imgCreationParams.GetParameter(ImageCreator.NameParameterName).Value = fileName;

//                imageClone = inImage.ImageFactory.CreateImage(imgCreationParams);
//            }
//            else
//            {
//                imageClone = inImage.ImageFactory.CreateTemporaryImage(imgCreationParams);
//            }

//            if (imageClone == null)
//                throw new Exception("Image could not be created.");

//            return imageClone;
//        }

//        private static ParameterList CreateOutputImageParameters(IImage inImage)
//        {
//            ParameterList imgCreationParams = inImage.ImageFactory.CreateParameters(null);
//            inImage.PopulateParameters(imgCreationParams, false);

//            if (!imgCreationParams.Contains(Image.PARAM_NO_DATA_VALUES.Name, ParamDirection.pt_any))
//            {
//                imgCreationParams.AddParameter(Image.PARAM_NO_DATA_VALUES);
//            }
//            Parameter noDataParam = imgCreationParams.GetParameter(Image.PARAM_NO_DATA_VALUES.Name);
//            object[] noDataVals = new object[1];
//            noDataVals[0] = 0;
//            noDataParam.Value = noDataVals;

//            if (imgCreationParams.Contains(ImageCreator.FillValuesParameterName, ParamDirection.pt_any))
//            {
//                Parameter fillValuesParam = imgCreationParams.GetParameter(ImageCreator.FillValuesParameterName);
//                object[] fillVals = new object[1];
//                fillVals[0] = noDataVals[0];
//                fillValuesParam.Value = fillVals;
//            }

//            Parameter imgPropParam = imgCreationParams.GetParameter(ImageCreator.ImagePropertiesParameterName);
//            ImageProperties imgProps = (ImageProperties)(imgPropParam.Value);
//            imgProps.VarType = typeof(UInt32);
//            imgProps.NumBands = 1;

//            return imgCreationParams;
//        }

//        /// <summary>
//        /// Processes the specified input image.
//        /// </summary>
//        /// <param name="inputImage">The input image.</param>
//        /// <param name="outputImage">The output image.</param>
//        /// <param name="maxHeightDiffBetweenPixels">The max height difference between pixels.</param>
//        /// <param name="minClusterSizeWithinTile">The min cluster size within each tile.</param>
//        /// <param name="neighborhoodType">Type of the neighborhood.</param>
//        /// <param name="outputType">Type of the output.</param>
//        /// <param name="progHook">The prog hook.</param>
//        /// <returns></returns>
//        public static SegmentationInfo Process(IImage inputImage, IMutableImage outputImage, float maxHeightDiffBetweenPixels, uint minClusterSizeWithinTile, NeighborhoodType neighborhoodType, OutputType outputType, ProgressStatus progHook)
//        {
//            if (inputImage == null)
//                throw new ArgumentNullException("inputImage");
//            if (outputImage == null)
//                throw new ArgumentNullException("outputImage");

//            if (inputImage.TileWidth != outputImage.TileWidth || inputImage.TileHeight != outputImage.TileHeight)
//                throw new ArgumentException(String.Format("Output tile size {0}x{1} does not match input {2}x{3}.", outputImage.TileWidth, outputImage.TileHeight, inputImage.TileWidth, inputImage.TileHeight), "outputImage");

//            uint currentClassIndexSpanningTiles = 0;

//            SortedList<uint, uint> classPixelCountsAcrossTiles = new SortedList<uint, uint>();
//            uint maxPixelCountAcrossTiles = 0;

//            using (SimpleImageTiling simpleTiling = new SimpleImageTiling())
//            {
//                simpleTiling.Add(inputImage, typeof(float));
//                simpleTiling.Add(outputImage);

//                bool convertInputData = !inputImage.VarType.Equals(typeof(float));
//                float noDataVal = (float)Convert.ChangeType(inputImage.NoDataValues[0], typeof(float));
//                const int sizeOutputOffset = 1;

//                // set up progress
//                int numTiles = (int)outputImage.NumTiles;
//                int operations = 3;
//                if (outputType == OutputType.Size)
//                    operations++;

//                if (progHook != null)
//                {
//                    progHook.SetRange(0, numTiles * 3);
//                    progHook.StepTo(0);
//                }

//                #region Classify one tile at a time

//                foreach (SimpleImageTileRegion tileRegion in simpleTiling)
//                {
//                    float[,] inputBuffer = simpleTiling.GetBuffer<float>(inputImage);
//                    uint[,] outputBuffer = simpleTiling.GetBuffer<uint>(outputImage);

//                    tileRegion.GetSampleTile(inputImage);

//                    currentClassIndexSpanningTiles = ClassifyTile(tileRegion, inputBuffer, outputBuffer, maxHeightDiffBetweenPixels, currentClassIndexSpanningTiles, minClusterSizeWithinTile, noDataVal, neighborhoodType);

//                    tileRegion.PutSampleTile(outputImage);

//                    if (progHook.Increment(1))
//                        throw new OWGAbortException();
//                }

//                #endregion

//                #region Merge tile classifications across tiles

//                Dictionary<uint, SortedList<uint, bool>> substitutionsAcrossTiles = new Dictionary<uint, SortedList<uint, bool>>();

//                PixelRegion rowTop;
//                rowTop.numRows = 1;
//                PixelRegion colLeft;
//                colLeft.numCols = 1;

//                foreach (SimpleImageTileRegion tileRegion in simpleTiling)
//                {
//                    float[,] inputBuffer = simpleTiling.GetBuffer<float>(inputImage);
//                    uint[,] outputBuffer = simpleTiling.GetBuffer<uint>(outputImage);

//                    tileRegion.GetSampleTile(inputImage);
//                    tileRegion.GetSampleTile(outputImage);

//                    rowTop.numCols = tileRegion.Cols;
//                    colLeft.numRows = tileRegion.Rows;

//                    Single[,] rowSliceBuffer1 = new Single[rowTop.numCols, 1];
//                    UInt32[,] rowSliceBuffer2 = new UInt32[rowTop.numCols, 1];
//                    Single[,] colSliceBuffer1 = new Single[1, colLeft.numRows];
//                    UInt32[,] colSliceBuffer2 = new UInt32[1, colLeft.numRows];

//                    Array originalRowSliceBuffer1 = rowSliceBuffer1;
//                    Array originalColSliceBuffer1 = colSliceBuffer1;

//                    if (convertInputData)
//                    {
//                        originalRowSliceBuffer1 = inputImage.CreateBuffer((uint)rowSliceBuffer1.GetLength(0), (uint)rowSliceBuffer1.GetLength(1));
//                        originalColSliceBuffer1 = inputImage.CreateBuffer((uint)colSliceBuffer1.GetLength(0), (uint)colSliceBuffer1.GetLength(1));
//                    }

//                    // check top edge
//                    if (tileRegion.PixelRow > 0)
//                    {
//                        rowTop.row = tileRegion.PixelRow - 1;
//                        rowTop.col = tileRegion.PixelCol;
//                        inputImage.GetSampleRegion(0, ref rowTop, originalRowSliceBuffer1);
//                        outputImage.GetSampleRegion(0, ref rowTop, rowSliceBuffer2);

//                        if (convertInputData)
//                            Image.ConvertToSingle(originalRowSliceBuffer1, inputImage.DataTypeCode, rowSliceBuffer1);

//                        MergeClassesFromAdjacentTiles(substitutionsAcrossTiles, inputBuffer, outputBuffer, rowSliceBuffer1, rowSliceBuffer2, tileRegion.Cols, maxHeightDiffBetweenPixels);
//                    }

//                    // check left edge
//                    if (tileRegion.PixelCol > 0)
//                    {
//                        colLeft.row = tileRegion.PixelRow;
//                        colLeft.col = tileRegion.PixelCol - 1;
//                        inputImage.GetSampleRegion(0, ref colLeft, originalColSliceBuffer1);
//                        outputImage.GetSampleRegion(0, ref colLeft, colSliceBuffer2);

//                        if (convertInputData)
//                            Image.ConvertToSingle(originalColSliceBuffer1, inputImage.DataTypeCode, colSliceBuffer1);

//                        MergeClassesFromAdjacentTiles(substitutionsAcrossTiles, inputBuffer, outputBuffer, colSliceBuffer1, colSliceBuffer2, tileRegion.Rows, maxHeightDiffBetweenPixels);
//                    }

//                    if (progHook.Increment(1))
//                        throw new OWGAbortException();
//                }

//                #endregion

//                #region Perform substitutions across tiles

//                foreach (SimpleImageTileRegion tileRegion in simpleTiling)
//                {
//                    uint[,] outputBuffer = simpleTiling.GetBuffer<uint>(outputImage);

//                    tileRegion.GetSampleTile(outputImage);

//                    for (int y = 0; y < tileRegion.Rows; y++)
//                    {
//                        for (int x = 0; x < tileRegion.Cols; x++)
//                        {
//                            uint currentClass = outputBuffer[x, y];
//                            if (currentClass > 0)
//                            {
//                                uint newClass = FindSubstitutePixelClass(substitutionsAcrossTiles, classPixelCountsAcrossTiles, ref maxPixelCountAcrossTiles, currentClass);
//                                uint reindexedClass = (uint)classPixelCountsAcrossTiles.IndexOfKey(newClass) + sizeOutputOffset;
//                                outputBuffer[x, y] = reindexedClass;
//                            }
//                        }
//                    }

//                    tileRegion.PutSampleTile(outputImage);

//                    if (progHook.Increment(1))
//                        throw new OWGAbortException();
//                }

//                #endregion

//                #region Save cluster size

//                if (outputType == OutputType.Size)
//                {
//                    foreach (SimpleImageTileRegion tileRegion in simpleTiling)
//                    {
//                        uint[,] outputBuffer = simpleTiling.GetBuffer<uint>(outputImage);

//                        tileRegion.GetSampleTile(outputImage);

//                        for (int y = 0; y < tileRegion.Rows; y++)
//                        {
//                            for (int x = 0; x < tileRegion.Cols; x++)
//                            {
//                                // at this point, currentClass is a re-indexed value
//                                uint indexPlusOffset = outputBuffer[x, y];
//                                if (indexPlusOffset >= sizeOutputOffset)
//                                {
//                                    outputBuffer[x, y] = classPixelCountsAcrossTiles.Values[(int)(indexPlusOffset - sizeOutputOffset)];
//                                }
//                            }
//                        }

//                        tileRegion.PutSampleTile(outputImage);

//                        if (progHook.Increment(1))
//                            throw new OWGAbortException();
//                    }
//                }

//                #endregion
//            }

//            SegmentationInfo info = new SegmentationInfo((uint)classPixelCountsAcrossTiles.Count, maxPixelCountAcrossTiles);
//            return info;
//        }

//        private static uint ClassifyTile(SimpleImageTileRegion tileRegion, float[,] inputBuffer, uint[,] outputBuffer, float maxHeightDiffBetweenPixels, uint currentClassIndexSpanningTiles, uint minClusterSize, float noDataVal, NeighborhoodType neighborhoodType)
//        {
//            UInt32 currentClassIndex = 2;

//            Dictionary<uint, SortedList<uint, bool>> substitutions = new Dictionary<uint, SortedList<uint, bool>>();
//            Dictionary<uint, uint> classPixelCounts = new Dictionary<uint, uint>();

//            for (int y = 0; y < tileRegion.Rows; y++)
//            {
//                for (int x = 0; x < tileRegion.Cols; x++)
//                {
//                    UInt32 newClassVal = 0;

//                    float thisVal = inputBuffer[x, y];

//                    // skip nodata values
//                    if (thisVal != noDataVal)
//                    {
//                        System.Drawing.Point[] coords = new System.Drawing.Point[4];
//                        int neighborIndex = 0;

//                        if (y > 0)
//                            coords[neighborIndex++] = new System.Drawing.Point(x, y - 1);

//                        if (x > 0)
//                            coords[neighborIndex++] = new System.Drawing.Point(x - 1, y);

//                        if (neighborhoodType == NeighborhoodType.EightNeighbors)
//                        {
//                            if (y > 0 && x > 0)
//                                coords[neighborIndex++] = new System.Drawing.Point(x - 1, y - 1);

//                            if (y > 0 && x < tileRegion.Cols - 1)
//                                coords[neighborIndex++] = new System.Drawing.Point(x + 1, y - 1);
//                        }

//                        for (int i = 0; i < neighborIndex; i++)
//                        {
//                            int neighborX = coords[i].X;
//                            int neighborY = coords[i].Y;

//                            float neighborVal = inputBuffer[neighborX, neighborY];
//                            if (neighborVal != noDataVal)
//                            {
//                                uint neighborClass = outputBuffer[neighborX, neighborY];
//                                if (System.Math.Abs(thisVal - neighborVal) <= maxHeightDiffBetweenPixels)
//                                {
//                                    if (newClassVal == 0)
//                                        newClassVal = neighborClass;
//                                    else
//                                        newClassVal = DetermineNewClassSubstitution(substitutions, newClassVal, neighborClass);
//                                }
//                            }
//                        }

//                        if (newClassVal == 0)
//                        {
//                            newClassVal = currentClassIndex;
//                            currentClassIndex++;
//                        }
//                    }
//                    outputBuffer[x, y] = newClassVal;
//                }
//            }

//            // perform substitutions
//            uint maxPixelCount = 0;
//            for (int y = 0; y < tileRegion.Rows; y++)
//            {
//                for (int x = 0; x < tileRegion.Cols; x++)
//                {
//                    uint currentClass = outputBuffer[x, y];
//                    if (currentClass > 0)
//                    {
//                        uint newClass = FindSubstitutePixelClass(substitutions, classPixelCounts, ref maxPixelCount, currentClass);
//                        outputBuffer[x, y] = newClass;
//                    }
//                }
//            }

//            // filter small clusters (trees and edges)
//            if (minClusterSize > 0)
//            {
//                for (int y = 0; y < tileRegion.Rows; y++)
//                {
//                    for (int x = 0; x < tileRegion.Cols; x++)
//                    {
//                        uint currentClass = outputBuffer[x, y];

//                        if (currentClass > 0)
//                        {
//                            uint count = classPixelCounts[currentClass];
//                            uint value = 0;

//                            value = currentClass;
//                            if (count < minClusterSize)
//                                value = 0;

//                            outputBuffer[x, y] = value;
//                        }
//                    }
//                }
//            }

//            // Update class indices to reduce max index values and remove duplicates across tiles.
//            // This is feasible because small clusters have been filtered out (set to zero).
//            Dictionary<uint, uint> classMappingUpdate = new Dictionary<uint, uint>();
//            for (int y = 0; y < tileRegion.Rows; y++)
//            {
//                for (int x = 0; x < tileRegion.Cols; x++)
//                {
//                    uint currentClass = outputBuffer[x, y];
//                    if (currentClass > 0)
//                    {
//                        if (!classMappingUpdate.ContainsKey(currentClass))
//                        {
//                            ++currentClassIndexSpanningTiles;
//                            classMappingUpdate.Add(currentClass, currentClassIndexSpanningTiles);
//                        }
//                        outputBuffer[x, y] = classMappingUpdate[currentClass];
//                    }
//                }
//            }

//            return currentClassIndexSpanningTiles;
//        }

//        private static uint FindSubstitutePixelClass(IDictionary<uint, SortedList<uint, bool>> substitutions, IDictionary<uint, uint> classPixelCounts, ref uint maxPixelCount, uint currentClass)
//        {
//            if (currentClass > 0)
//            {
//                if (substitutions.ContainsKey(currentClass))
//                {
//                    SortedList<uint, bool> substitutionSet = substitutions[currentClass];
//                    currentClass = substitutionSet.Keys[0];
//                }
//                if (!classPixelCounts.ContainsKey(currentClass))
//                    classPixelCounts.Add(currentClass, 0);

//                if (classPixelCounts[currentClass] < UInt32.MaxValue)
//                    classPixelCounts[currentClass]++;

//                if (classPixelCounts[currentClass] > maxPixelCount)
//                    maxPixelCount = classPixelCounts[currentClass];
//            }
//            return currentClass;
//        }

//        private static void MergeClassesFromAdjacentTiles(IDictionary<uint, SortedList<uint, bool>> substitutionsAcrossTiles, float[,] inputBuffer, uint[,] outputBuffer, float[,] sliceBuffer1, uint[,] sliceBuffer2, uint range, float maxHeightDiffBetweenPixels)
//        {
//            bool isRow = (sliceBuffer1.GetLength(0) > 1);

//            for (int i = 0; i < range; i++)
//            {
//                int x = 0;
//                int y = 0;

//                if (isRow)
//                    x = i;
//                else
//                    y = i;

//                float currentPixelValue = inputBuffer[x, y];
//                float otherPixelValue = sliceBuffer1[x, y];
//                uint currentPixelClass = outputBuffer[x, y];
//                uint otherPixelClass = sliceBuffer2[x, y];
//                float diff = System.Math.Abs(currentPixelValue - otherPixelValue);
//                if (currentPixelClass > 0 && otherPixelClass > 0 && currentPixelClass != otherPixelClass && diff <= maxHeightDiffBetweenPixels)
//                {
//                    DetermineNewClassSubstitution(substitutionsAcrossTiles, currentPixelClass, otherPixelClass);
//                }
//            }
//        }

//        private static uint DetermineNewClassSubstitution(IDictionary<uint, SortedList<uint, bool>> substitutions, uint newClassVal, uint compareClass)
//        {
//            if (newClassVal > 0 && compareClass > 0 && newClassVal != compareClass)
//            {
//                uint max = System.Math.Max(newClassVal, compareClass);
//                uint min = System.Math.Min(newClassVal, compareClass);

//                SortedList<uint, bool> substitutionSet = null;

//                if (substitutions.ContainsKey(max))
//                    substitutionSet = substitutions[max];

//                if (substitutions.ContainsKey(min))
//                {
//                    if (substitutionSet == null)
//                        substitutionSet = substitutions[min];
//                    else if (substitutionSet != substitutions[min])
//                    {
//                        // merge sets
//                        foreach (KeyValuePair<uint, bool> kvp in substitutions[min])
//                        {
//                            if (!substitutionSet.ContainsKey(kvp.Key))
//                            {
//                                substitutionSet.Add(kvp.Key, kvp.Value);
//                                substitutions[kvp.Key] = substitutionSet;
//                            }
//                        }
//                        substitutions[min] = substitutionSet;
//                    }
//                }

//                if (substitutionSet == null)
//                    substitutionSet = new SortedList<uint, bool>();

//                if (!substitutionSet.ContainsKey(max))
//                    substitutionSet.Add(max, true);
//                if (!substitutionSet.ContainsKey(min))
//                    substitutionSet.Add(min, true);

//                if (!substitutions.ContainsKey(max))
//                    substitutions.Add(max, substitutionSet);
//                if (!substitutions.ContainsKey(min))
//                    substitutions.Add(min, substitutionSet);
//            }
//            //else if (newClassVal == 0)
//            //    newClassVal = compareClass;
//            return newClassVal;
//        }
//    }
//}
