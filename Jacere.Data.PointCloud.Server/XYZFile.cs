using System;
using System.Collections.Generic;
using System.IO;

namespace Jacere.Data.PointCloud.Server
{
    class XyzFile : IPointSource
    {
        private static readonly double[] ReciprocalPowersOfTen;

        static XyzFile()
        {
            ReciprocalPowersOfTen = new double[19];
            for (var i = 0; i < ReciprocalPowersOfTen.Length; i++)
                ReciprocalPowersOfTen[i] = 1.0 / Math.Pow(10, i);
        }

        private readonly Stream _stream;

        public XyzFile(Stream stream)
        {
            _stream = stream;
        }

        public IEnumerable<Point3D> Points()
        {
            foreach (var point in IndexedPoints())
            {
                yield return point;
            }
        }

        public IEnumerable<IndexedPoint3D> IndexedPoints()
        {
            _stream.Seek(0, SeekOrigin.Begin);

            var buffer = new byte[4096];

            var skipped = 0;

            int bytesRead;
            var readStart = 0;

            while ((bytesRead = _stream.Read(buffer, readStart, buffer.Length - readStart)) > 0)
            {
                bytesRead += readStart;

                // find last line ending so that we can push everything after that to the next iteration
                var partialLinePos = bytesRead - 1;
                if (_stream.Position != _stream.Length)
                {
                    while (buffer[partialLinePos] != '\n')
                    {
                        --partialLinePos;
                    }
                }
                ++partialLinePos;
                    
                var i = 0;
                    
                while (i < partialLinePos)
                {
                    var lineStart = i;
                    var point = ParseXyzFromLine(buffer, ref i, partialLinePos);

                    if (point == null)
                    {
                        ++skipped;

                        if (skipped > 1000)
                        {
                            throw new Exception("failing to parse lines");
                        }

                        continue;
                    }

                    yield return new IndexedPoint3D(point.X, point.Y, point.Z, lineStart, partialLinePos - lineStart);
                }

                // handle buffer overlap
                readStart = buffer.Length - partialLinePos;
                Array.Copy(buffer, partialLinePos, buffer, 0, readStart);
            }

            if (skipped > 0)
            {
                Console.WriteLine($"skipped {skipped}");
            }
        }

        private static Point3D ParseXyzFromLine(byte[] buffer, ref int offset, int valid)
        {
            var hasX = ParseDouble(buffer, ref offset, valid, out var x);
            var hasY = ParseDouble(buffer, ref offset, valid, out var y);
            var hasZ = ParseDouble(buffer, ref offset, valid, out var z);
            
            while (offset < valid && buffer[offset] != '\n')
            {
                ++offset;
            }

            ++offset;

            return (hasX && hasY && hasZ) ? new Point3D(x, y, z) : null;
        }

        private static bool ParseDouble(byte[] buffer, ref int offset, int valid, out double value)
        {
            var sign = 1;
            var digits = 0L;
            var decimalPos = 0;
            var i = offset;

            if (buffer[i] == '-')
            {
                sign = -1;
                ++i;
            }

            while (i < valid && buffer[i] >= ',' && buffer[i] <= '9')
            {
                // todo: add ',' support and fail on '/'
                if (buffer[i] == '.')
                {
                    decimalPos = i;
                }
                else
                {
                    digits = 10 * digits + (buffer[i] - '0');
                }

                ++i;
            }

            offset = i + 1;
            
            if (decimalPos != 0 && (i == valid || buffer[i] == '\r' || buffer[i] == '\n' || buffer[i] == ' '))
            {
                value = sign * digits * ReciprocalPowersOfTen[i - decimalPos - 1];
                return true;
            }

            value = 0;
            return false;
        }
    }
}
