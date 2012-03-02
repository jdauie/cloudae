using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.IO;

using CloudAE.Core.Geometry;
using CloudAE.Core.Compression;

namespace CloudAE.Core
{
	class XYZFile : FileHandlerBase
	{
		private const int POINT_SIZE_BYTES = 3 * sizeof(double);
		private const int POINTS_PER_BUFFER = BufferManager.BUFFER_SIZE_BYTES / POINT_SIZE_BYTES;
		private const int USABLE_BYTES_PER_BUFFER = POINTS_PER_BUFFER * POINT_SIZE_BYTES;

		public XYZFile(string path)
			: base(path)
		{
		}

		public override PointCloudBinarySource GenerateBinarySource(ProgressManager progressManager)
		{
			string binaryPath = ProcessingSet.GetBinarySourceName(this);
			return ConvertTextToBinary(binaryPath, progressManager);
		}

		public override string GetPreview()
		{
			IEnumerable<string> lines = File.ReadLines(FilePath).Take(10);
			return string.Join("\n", lines);
		}

		public unsafe PointCloudBinarySource ConvertTextToBinary(string binaryPath, ProgressManager progressManager)
		{
			Stopwatch stopwatch = new Stopwatch();
			stopwatch.Start();

			byte[] buffer = new byte[BufferManager.BUFFER_SIZE_BYTES];
			int bufferIndex = 0;
			int pointCount = 0;
			int skipped = 0;

			double minX = 0, minY = 0, minZ = 0;
			double maxX = 0, maxY = 0, maxZ = 0;

			fixed (byte* outputBufferPtr = buffer)
			{
				using (FileStream outputStream = new FileStream(binaryPath, FileMode.Create, FileAccess.Write, FileShare.None, BufferManager.BUFFER_SIZE_BYTES))
				{
					FileStream inputStream = new FileStream(FilePath, FileMode.Open, FileAccess.Read, FileShare.Read, BufferManager.BUFFER_SIZE_BYTES);
					long inputLength = inputStream.Length;

					long estimatedOutputLength = (long)(0.5 * inputLength);
					outputStream.SetLength(estimatedOutputLength);

					using (StreamReader reader = new StreamReader(inputStream))
					{
						string line;

						double x = 0.0;
						double y = 0.0;
						double z = 0.0;

						while ((line = reader.ReadLine()) != null)
						{
							if (!ParseXYZFromLine(line, ref x, ref y, ref z))
							{
								++skipped;
								continue;
							}

							if (pointCount == 0)
							{
								minX = maxX = x;
								minY = maxY = y;
								minZ = maxZ = z;
							}
							else
							{
								if (x < minX) minX = x; else if (x > maxX) maxX = x;
								if (y < minY) minY = y; else if (y > maxY) maxY = y;
								if (z < minZ) minZ = z; else if (z > maxZ) maxZ = z;
							}

							double* p = (double*)(outputBufferPtr + bufferIndex);

							p[0] = x;
							p[1] = y;
							p[2] = z;

							bufferIndex += POINT_SIZE_BYTES;
							++pointCount;

							// write usable buffer chunk
							if (USABLE_BYTES_PER_BUFFER == bufferIndex)
							{
								outputStream.Write(buffer, 0, bufferIndex);
								bufferIndex = 0;

								if (!progressManager.Update((float)inputStream.Position / inputLength))
									break;
							}
						}

						// write remaining buffer
						if (bufferIndex > 0)
						{
							outputStream.Write(buffer, 0, bufferIndex);
						}

						progressManager.Update((float)inputStream.Position / inputLength);
					}

					outputStream.SetLength(outputStream.Position);
				}
			}

			progressManager.Log("Skipped {0} lines", skipped);
			progressManager.Log(stopwatch, "Copied {0:0,0} points", pointCount);

			Extent3D extent = new Extent3D(minX, minY, minZ, maxX, maxY, maxZ);

			PointCloudBinarySource source = new PointCloudBinarySource(binaryPath, pointCount, extent, null, 0, POINT_SIZE_BYTES, CompressionMethod.None);

			return source;
		}

		private bool ParseXYZFromLine(string line, ref double x, ref double y, ref double z)
		{
			// get the first three floats
			unsafe
			{
				int length = line.Length;
				int position = 0;

				fixed (char* characters = line)
				{
					if (!ParseNextDoubleFromLine(characters, ref position, length, ref x)) return false;
					if (!ParseNextDoubleFromLine(characters, ref position, length, ref y)) return false;
					if (!ParseNextDoubleFromLine(characters, ref position, length, ref z)) return false;
				}
			}

			return true;
		}

		private unsafe bool ParseNextDoubleFromLine(char* characters, ref int position, int length, ref double value)
		{
			bool isValid = false;

			value = 0.0;
			int decimalSeperatorPosition = -1;
			for (; position < length; ++position)
			{
				char c = characters[position];
				if (c < '0' || c > '9')
				{
					if (c == '.')
					{
						decimalSeperatorPosition = position;
						continue;
					}
					if (value > 0)
						break;
				}
				else
				{
					value = 10 * value + (c - '0');
					isValid = true;
				}
			}

			if (value > 0.0 && decimalSeperatorPosition != -1)
			{
				for (int i = decimalSeperatorPosition + 1; i < position; ++i)
					value /= 10;
			}

			return isValid;
		}
	}
}
