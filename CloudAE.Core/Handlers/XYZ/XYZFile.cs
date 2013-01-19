using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;

using Jacere.Core.Geometry;

namespace CloudAE.Core
{
	class XYZFile : FileHandlerBase
	{
		private static readonly double[] c_reciprocalPowersOfTen;

		static XYZFile()
		{
			c_reciprocalPowersOfTen = new double[19];
			for (int i = 0; i < c_reciprocalPowersOfTen.Length; i++)
				c_reciprocalPowersOfTen[i] = 1.0 / Math.Pow(10, i);
		}

		public XYZFile(string path)
			: base(path)
		{
		}

		public override IPointCloudBinarySource GenerateBinarySource(ProgressManager progressManager)
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
			short pointSizeBytes = 3 * sizeof(double);

			double minX = 0, minY = 0, minZ = 0;
			double maxX = 0, maxY = 0, maxZ = 0;
			int pointCount = 0;

			using (var process = progressManager.StartProcess("ConvertTextToBinary"))
			{
				BufferInstance inputBuffer = process.AcquireBuffer(true);
				BufferInstance outputBuffer = process.AcquireBuffer(true);

				int pointsPerBuffer = outputBuffer.Length / pointSizeBytes;
				int usableBytesPerBuffer = pointsPerBuffer * pointSizeBytes;

				byte* inputBufferPtr = inputBuffer.DataPtr;
				byte* outputBufferPtr = outputBuffer.DataPtr;

				int bufferIndex = 0;
				int skipped = 0;

				using (var inputStream = StreamManager.OpenReadStream(FilePath))
				{
					long inputLength = inputStream.Length;
					long estimatedOutputLength = inputLength;

					using (var outputStream = StreamManager.OpenWriteStream(binaryPath, estimatedOutputLength, 0, true))
					{
						int bytesRead;
						int readStart = 0;

						while ((bytesRead = inputStream.Read(inputBuffer.Data, readStart, inputBuffer.Length - readStart)) > 0)
						{
							bytesRead += readStart;
							readStart = 0;
							int i = 0;

							while (i < bytesRead)
							{
								// identify line start
								while (i < bytesRead && (inputBufferPtr[i] == '\r' || inputBufferPtr[i] == '\n'))
								{
									++i;
								}

								int lineStart = i;

								// identify line end
								while (i < bytesRead && inputBufferPtr[i] != '\r' && inputBufferPtr[i] != '\n')
								{
									++i;
								}

								// handle buffer overlap
								if (i == bytesRead)
								{
									Array.Copy(inputBuffer.Data, lineStart, inputBuffer.Data, 0, i - lineStart);
									readStart = i - lineStart;
									break;
								}

								// this may get overwritten if this is not a valid parse
								double* p = (double*)(outputBufferPtr + bufferIndex);

								if (!ParseXYZFromLine(inputBufferPtr, lineStart, i, p))
								{
									++skipped;
									continue;
								}

								if (pointCount == 0)
								{
									minX = maxX = p[0];
									minY = maxY = p[1];
									minZ = maxZ = p[2];
								}
								else
								{
									if (p[0] < minX) minX = p[0];
									else if (p[0] > maxX) maxX = p[0];
									if (p[1] < minY) minY = p[1];
									else if (p[1] > maxY) maxY = p[1];
									if (p[2] < minZ) minZ = p[2];
									else if (p[2] > maxZ) maxZ = p[2];
								}

								bufferIndex += pointSizeBytes;
								++pointCount;

								// write usable buffer chunk
								if (usableBytesPerBuffer == bufferIndex)
								{
									outputStream.Write(outputBuffer.Data, 0, bufferIndex);
									bufferIndex = 0;
								}
							}

							if (!process.Update((float)inputStream.Position / inputLength))
								break;
						}

						// write remaining buffer
						if (bufferIndex > 0)
							outputStream.Write(outputBuffer.Data, 0, bufferIndex);
					}
				}

				process.Log("Skipped {0} lines", skipped);
				process.LogTime("Copied {0:0,0} points", pointCount);
			}

			var extent = new Extent3D(minX, minY, minZ, maxX, maxY, maxZ);

			var source = new PointCloudBinarySource(this, pointCount, extent, null, 0, pointSizeBytes);

			return source;
		}

		private unsafe bool ParseXYZFromLine(byte* bufferPtr, int startPos, int endPos, double* xyz)
		{
			for (int i = 0; i < 3; i++)
			{
				long digits = 0;

				// find start
				while (startPos < endPos && (bufferPtr[startPos] < '0' || bufferPtr[startPos] > '9'))
				{
					++startPos;
				}

				// accumulate digits (before decimal separator)
				int currentStartPos = startPos;
				while (startPos < endPos && (bufferPtr[startPos] >= '0' && bufferPtr[startPos] <= '9'))
				{
					digits = 10 * digits + (bufferPtr[startPos] - '0');
					++startPos;
				}

				// check for decimal separator
				if (startPos > currentStartPos && startPos < endPos && bufferPtr[startPos] == '.')
				{
					int decimalSeperatorPosition = startPos;
					++startPos;

					// accumulate digits (after decimal separator)
					while (startPos < endPos && (bufferPtr[startPos] >= '0' && bufferPtr[startPos] <= '9'))
					{
						digits = 10 * digits + (bufferPtr[startPos] - '0');
						++startPos;
					}

					xyz[i] = digits * c_reciprocalPowersOfTen[startPos - decimalSeperatorPosition - 1];
				}
				else
				{
					xyz[i] = digits;
				}

				if (startPos == currentStartPos || digits < 0)
				{
					// no digits or too many (overflow)
					return false;
				}
			}

			return true;
		}
	}
}
