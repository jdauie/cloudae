using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CloudAE.Core
{
	/// <summary>
	/// Specifies the neighbor comparison method to use for surrounding pixels.
	/// </summary>
	/// <code>
	/// -------------------
	/// | TL  |  T  |  TR |
	/// -------------------
	/// |  L  |  C  |     |
	/// -------------------
	/// |     |     |     |
	/// -------------------
	/// </code>
	public enum NeighborhoodType
	{
		/// <summary>Compares the current pixel to Top and Left only.</summary>
		FourNeighbors,
		/// <summary>Compares the current pixel to Top, Left, TopLeft, and TopRight.</summary>
		EightNeighbors
	};

	public class Segmentation
	{
		public static uint ClassifyTile(Grid<float> inputBuffer, uint[,] outputBuffer, float maxHeightDiffBetweenPixels, uint currentClassIndexSpanningTiles, uint minClusterSize, float noDataVal, NeighborhoodType neighborhoodType)
		{
			int width = inputBuffer.SizeX;
			int height = inputBuffer.SizeY;

			UInt32 currentClassIndex = 2;

			Dictionary<uint, SortedList<uint, bool>> substitutions = new Dictionary<uint, SortedList<uint, bool>>();
			Dictionary<uint, uint> classPixelCounts = new Dictionary<uint, uint>();

			for (int y = 0; y < height; y++)
			{
				for (int x = 0; x < width; x++)
				{
					UInt32 newClassVal = 0;

					float thisVal = inputBuffer.Data[x, y];

					// skip nodata values
					if (thisVal != noDataVal)
					{
						System.Drawing.Point[] coords = new System.Drawing.Point[4];
						int neighborIndex = 0;

						if (y > 0)
							coords[neighborIndex++] = new System.Drawing.Point(x, y - 1);

						if (x > 0)
							coords[neighborIndex++] = new System.Drawing.Point(x - 1, y);

						if (neighborhoodType == NeighborhoodType.EightNeighbors)
						{
							if (y > 0 && x > 0)
								coords[neighborIndex++] = new System.Drawing.Point(x - 1, y - 1);

							if (y > 0 && x < width - 1)
								coords[neighborIndex++] = new System.Drawing.Point(x + 1, y - 1);
						}

						for (int i = 0; i < neighborIndex; i++)
						{
							int neighborX = coords[i].X;
							int neighborY = coords[i].Y;

							float neighborVal = inputBuffer.Data[neighborX, neighborY];
							if (neighborVal != noDataVal)
							{
								uint neighborClass = outputBuffer[neighborX, neighborY];
								if (System.Math.Abs(thisVal - neighborVal) <= maxHeightDiffBetweenPixels)
								{
									if (newClassVal == 0)
										newClassVal = neighborClass;
									else
										newClassVal = DetermineNewClassSubstitution(substitutions, newClassVal, neighborClass);
								}
							}
						}

						if (newClassVal == 0)
						{
							newClassVal = currentClassIndex;
							currentClassIndex++;
						}
					}
					outputBuffer[x, y] = newClassVal;
				}
			}

			// perform substitutions
			uint maxPixelCount = 0;
			for (int y = 0; y < height; y++)
			{
				for (int x = 0; x < width; x++)
				{
					uint currentClass = outputBuffer[x, y];
					if (currentClass > 0)
					{
						uint newClass = FindSubstitutePixelClass(substitutions, classPixelCounts, ref maxPixelCount, currentClass);
						outputBuffer[x, y] = newClass;
					}
				}
			}

			// filter small clusters (trees and edges)
			if (minClusterSize > 0)
			{
				for (int y = 0; y < height; y++)
				{
					for (int x = 0; x < width; x++)
					{
						uint currentClass = outputBuffer[x, y];

						if (currentClass > 0)
						{
							uint count = classPixelCounts[currentClass];
							uint value = 0;

							value = currentClass;
							if (count < minClusterSize)
								value = 0;

							outputBuffer[x, y] = value;
						}
					}
				}
			}

			// Update class indices to reduce max index values and remove duplicates across tiles.
			// This is feasible because small clusters have been filtered out (set to zero).
			Dictionary<uint, uint> classMappingUpdate = new Dictionary<uint, uint>();
			for (int y = 0; y < height; y++)
			{
				for (int x = 0; x < width; x++)
				{
					uint currentClass = outputBuffer[x, y];
					if (currentClass > 0)
					{
						if (!classMappingUpdate.ContainsKey(currentClass))
						{
							++currentClassIndexSpanningTiles;
							classMappingUpdate.Add(currentClass, currentClassIndexSpanningTiles);
						}
						outputBuffer[x, y] = classMappingUpdate[currentClass];
					}
				}
			}

			return currentClassIndexSpanningTiles;
		}

		private static uint FindSubstitutePixelClass(IDictionary<uint, SortedList<uint, bool>> substitutions, IDictionary<uint, uint> classPixelCounts, ref uint maxPixelCount, uint currentClass)
		{
			if (currentClass > 0)
			{
				if (substitutions.ContainsKey(currentClass))
				{
					SortedList<uint, bool> substitutionSet = substitutions[currentClass];
					currentClass = substitutionSet.Keys[0];
				}
				if (!classPixelCounts.ContainsKey(currentClass))
					classPixelCounts.Add(currentClass, 0);

				if (classPixelCounts[currentClass] < UInt32.MaxValue)
					classPixelCounts[currentClass]++;

				if (classPixelCounts[currentClass] > maxPixelCount)
					maxPixelCount = classPixelCounts[currentClass];
			}
			return currentClass;
		}

		private static uint DetermineNewClassSubstitution(IDictionary<uint, SortedList<uint, bool>> substitutions, uint newClassVal, uint compareClass)
		{
			if (newClassVal > 0 && compareClass > 0 && newClassVal != compareClass)
			{
				uint max = System.Math.Max(newClassVal, compareClass);
				uint min = System.Math.Min(newClassVal, compareClass);

				SortedList<uint, bool> substitutionSet = null;

				if (substitutions.ContainsKey(max))
					substitutionSet = substitutions[max];

				if (substitutions.ContainsKey(min))
				{
					if (substitutionSet == null)
						substitutionSet = substitutions[min];
					else if (substitutionSet != substitutions[min])
					{
						// merge sets
						foreach (KeyValuePair<uint, bool> kvp in substitutions[min])
						{
							if (!substitutionSet.ContainsKey(kvp.Key))
							{
								substitutionSet.Add(kvp.Key, kvp.Value);
								substitutions[kvp.Key] = substitutionSet;
							}
						}
						substitutions[min] = substitutionSet;
					}
				}

				if (substitutionSet == null)
					substitutionSet = new SortedList<uint, bool>();

				if (!substitutionSet.ContainsKey(max))
					substitutionSet.Add(max, true);
				if (!substitutionSet.ContainsKey(min))
					substitutionSet.Add(min, true);

				if (!substitutions.ContainsKey(max))
					substitutions.Add(max, substitutionSet);
				if (!substitutions.ContainsKey(min))
					substitutions.Add(min, substitutionSet);
			}

			return newClassVal;
		}
	}
}
