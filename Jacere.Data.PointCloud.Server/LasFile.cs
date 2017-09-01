using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Jacere.Data.PointCloud.Server
{
    public interface IPointSource
    {
        IEnumerable<IndexedPoint3D> Points();
    }

    class LasFile : IPointSource
    {
        private readonly Stream _stream;
        
        private readonly LasHeader _header;

        private readonly LasVlr[] _vlrs;
        private readonly LasEvlr[] _evlrs;
        
        public LasFile(Stream stream)
        {
            _stream = stream;

            using (var reader = new BinaryReader(_stream, Encoding.UTF8, true))
            {
                _header = reader.ReadLasHeader();
            }

            _vlrs = _header.ReadVlrs(stream);
            _evlrs = _header.ReadEvlrs(stream);
        }

        public IEnumerable<IndexedPoint3D> Points()
        {
            _stream.Seek(_header.OffsetToPointData, SeekOrigin.Begin);

            var buffer = new byte[(int)ByteSizesSmall.MB_1];

            var points = new List<Point3D>();

            var bytesRemaining = _header.PointDataRecordLength * _header.PointCount;
            var wholePointsInBuffer = Math.Min(buffer.Length, _header.PointDataRecordLength);
            var usableBytesPerBuffer = (ulong)wholePointsInBuffer * _header.PointDataRecordLength;

            while (bytesRemaining > 0)
            {
                var streamPos = _stream.Position;
                var bytesRead = _stream.ReadExact(buffer, 0, (int)Math.Min(usableBytesPerBuffer, bytesRemaining));

                GetPoints(buffer, bytesRead, points);

                foreach (var point in points)
                {
                    yield return new IndexedPoint3D(point.X, point.Y, point.Z, streamPos, _header.PointDataRecordLength);
                    streamPos += _header.PointDataRecordLength;
                }

                points.Clear();

                bytesRemaining -= (ulong)bytesRead;
            }
        }

        private unsafe void GetPoints(byte[] buffer, int bytesRead, ICollection<Point3D> points)
        {
            fixed (byte* pb = buffer)
            {
                var pbEnd = pb + bytesRead;
                var p = (SQuantizedPoint3D*) pb;
                while (p < pbEnd)
                {
                    points.Add(_header.Quantization.Convert(*p));

                    p += _header.PointDataRecordLength;
                }
            }
        }
    }
}
