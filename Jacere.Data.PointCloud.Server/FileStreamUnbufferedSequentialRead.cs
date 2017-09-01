using System;
using System.IO;

namespace Jacere.Data.PointCloud.Server
{
    public enum ByteSizesSmall
    {
        KB_4 = 1 << 12,
        KB_8 = 1 << 13,
        KB_16 = 1 << 14,
        KB_32 = 1 << 15,
        KB_64 = 1 << 16,
        KB_128 = 1 << 17,
        KB_256 = 1 << 18,
        KB_512 = 1 << 19,
        MB_1 = 1 << 20,
        MB_2 = 1 << 21,
        MB_4 = 1 << 22,
        MB_8 = 1 << 23,
        MB_16 = 1 << 24,
        MB_32 = 1 << 25,
        MB_64 = 1 << 26,
        MB_128 = 1 << 27,
        MB_256 = 1 << 28,
        MB_512 = 1 << 29,
        GB_1 = 1 << 30,
    }

    public enum ByteSizesLarge : long
    {
        MB_1 = (long)1 << 20,
        MB_2 = (long)1 << 21,
        MB_4 = (long)1 << 22,
        MB_8 = (long)1 << 23,
        MB_16 = (long)1 << 24,
        MB_32 = (long)1 << 25,
        MB_64 = (long)1 << 26,
        MB_128 = (long)1 << 27,
        MB_256 = (long)1 << 28,
        MB_512 = (long)1 << 29,
        GB_1 = (long)1 << 30,
        GB_2 = (long)1 << 31,
        GB_4 = (long)1 << 32,
    }

    public class FileStreamUnbufferedSequentialRead : Stream
    {
        private const FileOptions FileFlagNoBuffering = (FileOptions)0x20000000;

        private const int BufferSize = (int)ByteSizesSmall.MB_1;
        private const int SectorSize = (int)ByteSizesSmall.KB_4;

        private byte[] _buffer;
        private FileStream _stream;
        private FileStream _streamEnd;
        private long _streamPosition;
        private int _bufferIndex;
        private bool _bufferIsValid;
        private int _bufferValidSize;

        public string Path { get; }

        public FileStreamUnbufferedSequentialRead(string path)
            : this(path, 0)
        {
        }

        public FileStreamUnbufferedSequentialRead(string path, long startPosition)
        {
            Path = path;

            // todo: where should this buffer size come from?
            _buffer = new byte[BufferSize];
            _bufferValidSize = _buffer.Length;

            const FileMode mode = FileMode.Open;
            const FileAccess access = FileAccess.Read;
            const FileShare share = FileShare.Read;
            const FileOptions options = FileFlagNoBuffering | FileOptions.WriteThrough | FileOptions.SequentialScan;

            _stream = new FileStream(Path, mode, access, share, BufferSize, options);
            _streamEnd = new FileStream(Path, mode, access, share, BufferSize, FileOptions.WriteThrough);

            Seek(startPosition);
        }

        private static long GetPositionAligned(long position)
        {
            return position == 0
                ? 0
                : ((position + (SectorSize - 1)) & ~(SectorSize - 1)) - SectorSize;
        }

        public void Seek(long position)
        {
            if (Position == position)
            {
                return;
            }

            var positionAligned = GetPositionAligned(position);
            _stream.Seek(positionAligned, SeekOrigin.Begin);
            _bufferIndex = (int)(position - positionAligned);
            _streamPosition = positionAligned;
            _bufferIsValid = false;
            _bufferValidSize = _buffer.Length;
        }

        public override int Read(byte[] array, int offset, int count)
        {
            var startingOffset = offset;
            var bytesToRead = count;

            while (bytesToRead > 0)
            {
                if (!_bufferIsValid || _bufferIndex == _buffer.Length)
                    ReadInternal();

                // copy from array into remaining buffer
                var remainingDataInBuffer = _bufferValidSize - _bufferIndex;
                var bytesToCopy = Math.Min(remainingDataInBuffer, bytesToRead);

                Buffer.BlockCopy(_buffer, _bufferIndex, array, offset, bytesToCopy);
                _bufferIndex += bytesToCopy;
                offset += bytesToCopy;
                bytesToRead -= bytesToCopy;

                if (_bufferValidSize < _buffer.Length)
                {
                    break;
                }
            }

            return offset - startingOffset;
        }

        private void ReadInternal()
        {
            // a partial read is required at the end of the file
            var position = _streamPosition;
            if (position + _buffer.Length > _stream.Length)
            {
                _streamEnd.Seek(position, SeekOrigin.Begin);
                _bufferValidSize = _streamEnd.Read(_buffer, 0, (int)(_streamEnd.Length - position));
            }
            else
            {
                _stream.Read(_buffer, 0, _buffer.Length);
                _streamPosition += _buffer.Length;
                _bufferValidSize = _buffer.Length;
            }

            // if the buffer was not valid, we just did a seek
            // and need to maintain the buffer index
            if (_bufferIsValid)
            {
                _bufferIndex = 0;
            }
            else
            {
                _bufferIsValid = true;
            }
        }
        
        protected override void Dispose(bool disposing)
        {
            if (_stream != null)
            {
                _stream.Dispose();
                _stream = null;
            }

            if (_streamEnd != null)
            {
                _streamEnd.Dispose();
                _streamEnd = null;
            }

            _buffer = null;
        }

        public override bool CanRead => true;

        public override bool CanSeek => true;

        public override bool CanWrite => false;

        public override long Length => _stream.Length;

        public override long Position
        {
            get
            {
                // end of file
                if (_bufferValidSize != _buffer.Length)
                    return _streamPosition + _bufferIndex;

                // normal
                if (_bufferIsValid)
                    return _streamPosition - (_buffer.Length - _bufferIndex);

                // beginning of file
                return _streamPosition + _bufferIndex;
            }
            set => Seek(value);
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            long actualOffset;

            switch (origin)
            {
                case SeekOrigin.Begin:
                    actualOffset = offset;
                    break;
                case SeekOrigin.Current:
                    actualOffset = offset + Position;
                    break;
                default:
                    throw new ArgumentException("Unsupported SeekOrigin");
            }

            Seek(actualOffset);

            return actualOffset;
        }

        public override void SetLength(long value)
        {
            throw new InvalidOperationException("Cannot SetLength read-only stream");
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new InvalidOperationException("Cannot Write read-only stream");
        }

        public override void Flush()
        {
            throw new InvalidOperationException("Cannot Flush read-only stream");
        }
    }
}
