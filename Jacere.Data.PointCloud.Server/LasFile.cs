using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace Jacere.Data.PointCloud.Server
{
    public interface IPointSource
    {
        IEnumerable<Point3D> Points();
        IEnumerable<IndexedPoint3D> IndexedPoints();
    }

    public class LasFile : IPointSource
    {
        public readonly Stream Stream;
        
        public readonly LasHeader Header;

        private readonly LasVlr[] _vlrs;
        private readonly LasEvlr[] _evlrs;
        
        public LasFile(Stream stream)
        {
            Stream = stream;

            using (var reader = new BinaryReader(Stream, Encoding.UTF8, true))
            {
                Header = reader.ReadLasHeader();
            }

            _vlrs = Header.ReadVlrs(stream);
            _evlrs = Header.ReadEvlrs(stream);
        }

        public IEnumerable<Point3D> Points()
        {
            return new LasFileEnumerator<Point3D>(this);
        }

        public IEnumerable<IndexedPoint3D> IndexedPoints()
        {
            return new LasFileIndexEnumerator<IndexedPoint3D>(this);
        }

        //public IEnumerable<Point3D> Points2()
        //{
        //    Stream.Seek(Header.OffsetToPointData, SeekOrigin.Begin);

        //    var buffer = new byte[(int)ByteSizesSmall.MB_1];

        //    var points = new List<Point3D>();

        //    var bytesRemaining = Header.PointDataRecordLength * Header.PointCount;
        //    var wholePointsInBuffer = buffer.Length / Header.PointDataRecordLength;
        //    var usableBytesPerBuffer = (ulong)wholePointsInBuffer * Header.PointDataRecordLength;

        //    while (bytesRemaining > 0)
        //    {
        //        var streamPos = Stream.Position;
        //        var bytesRead = Stream.ReadExact(buffer, 0, (int)Math.Min(usableBytesPerBuffer, bytesRemaining));

        //        GetPoints(buffer, bytesRead, points);

        //        foreach (var point in points)
        //        {
        //            yield return point;
        //            //yield return new Point3D(point.X, point.Y, point.Z, streamPos, _header.PointDataRecordLength);
        //            //streamPos += _header.PointDataRecordLength;
        //        }

        //        points.Clear();

        //        bytesRemaining -= (ulong)bytesRead;
        //    }
        //}

        //private unsafe void GetPoints(byte[] buffer, int bytesRead, ICollection<Point3D> points)
        //{
        //    fixed (byte* pb = buffer)
        //    {
        //        var pbEnd = pb + bytesRead;
        //        var p = pb;
        //        while (p < pbEnd)
        //        {
        //            var p2 = *(SQuantizedPoint3D*) p;
        //            points.Add(new Point3D(
        //                p2.X,// * _header.Quantization.ScaleFactorX + _header.Quantization.OffsetX, 
        //                p2.Y,// * _header.Quantization.ScaleFactorY + _header.Quantization.OffsetY, 
        //                p2.Z // * _header.Quantization.ScaleFactorZ + _header.Quantization.OffsetZ
        //            ));
        //            //points.Add(_header.Quantization.Convert(*(SQuantizedPoint3D*)p));

        //            p += Header.PointDataRecordLength;
        //        }
        //    }
        //}
    }

    public unsafe class LasFileIndexEnumerator<T> : LasFileEnumerator<T>
        where T : Point3D
    {
        public override T Current
        {
            get
            {
                var p = *(SQuantizedPoint3D*)_p;
                return new IndexedPoint3D(
                    p.X * _file.Header.Quantization.ScaleFactorX + _file.Header.Quantization.OffsetX,
                    p.Y * _file.Header.Quantization.ScaleFactorY + _file.Header.Quantization.OffsetY,
                    p.Z * _file.Header.Quantization.ScaleFactorZ + _file.Header.Quantization.OffsetZ,
                    _streamPos,
                    _file.Header.PointDataRecordLength
                ) as T;
            }
        }

        public LasFileIndexEnumerator(LasFile file) : base(file)
        {
        }
    }

    public unsafe class LasFileEnumerator<T> : IEnumerator<T>, IEnumerable<T>
        where T : Point3D
    {
        private const int BufferSize = (int)ByteSizesSmall.MB_1;

        protected readonly LasFile _file;
        private readonly byte[] _buffer;

        private readonly long _streamPosEnd;
        protected long _streamPos;

        private GCHandle _handle;
        private byte* _pStart;
        private byte* _pEnd;
        protected byte* _p;

        public virtual T Current
        {
            get
            {
                var p = *(SQuantizedPoint3D*)_p;
                return new Point3D(
                    p.X * _file.Header.Quantization.ScaleFactorX + _file.Header.Quantization.OffsetX,
                    p.Y * _file.Header.Quantization.ScaleFactorY + _file.Header.Quantization.OffsetY,
                    p.Z * _file.Header.Quantization.ScaleFactorZ + _file.Header.Quantization.OffsetZ
                ) as T;
            }
        }

        object IEnumerator.Current => Current;

        public LasFileEnumerator(LasFile file)
        {
            _file = file;
            _buffer = new byte[BufferSize / _file.Header.PointDataRecordLength * _file.Header.PointDataRecordLength];

            _file.Stream.Seek(_file.Header.OffsetToPointData, SeekOrigin.Begin);
            
            _streamPosEnd = (long)(_file.Header.OffsetToPointData + _file.Header.PointDataRecordLength * _file.Header.PointCount);

            _handle = GCHandle.Alloc(_buffer, GCHandleType.Pinned);
            var pAddr = Marshal.UnsafeAddrOfPinnedArrayElement(_buffer, 0);

            _pStart = (byte*)pAddr.ToPointer();
            _pEnd = _pStart;
            _p = _pStart;

            MoveNextChunk();
        }

        public bool MoveNext()
        {
            _p += _file.Header.PointDataRecordLength;
            _streamPos += _file.Header.PointDataRecordLength;

            return _p != _pEnd || MoveNextChunk();
        }

        private bool MoveNextChunk()
        {
            _streamPos = _file.Stream.Position;

            if (_streamPos == _streamPosEnd)
            {
                return false;
            }

            var remainingBytes = (int) Math.Min(_buffer.Length, _streamPosEnd - _streamPos);
            var bytesRead = _file.Stream.ReadExact(_buffer, 0, remainingBytes);

            _pEnd = _pStart + bytesRead;
            _p = _pStart;

            if (_streamPos == _file.Header.OffsetToPointData)
            {
                // this is an invalid location, but it makes the MoveNext increment convenient
                _p -= _file.Header.PointDataRecordLength;
            }

            return true;
        }

        public void Reset()
        {
            throw new NotSupportedException();
        }

        public IEnumerator<T> GetEnumerator()
        {
            while (MoveNext())
            {
                yield return Current;
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Dispose()
        {
            _p = null;
            _pStart = null;
            _pEnd = null;

            if (_handle.IsAllocated)
                _handle.Free();
        }
    }
}
