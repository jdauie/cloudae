using System;
using System.Collections.Generic;
using System.Linq;

using Jacere.Core;
using System.IO;
using Jacere.Core.Util;

namespace CloudAE.Core
{
	public class GridTileSource<T> : IDisposable where T : struct
	{
		public readonly string FilePath;

		// order tiles across, then down

		public readonly int TilesX;
		public readonly int TilesY;
		public readonly int TileSizeX;
		public readonly int TileSizeY;
		public readonly int TileSizePixels;
		public readonly int TileSizeBytes;

		public readonly int SizeX;
		public readonly int SizeY;
		public readonly long SizePixels;
		public readonly long SizeBytes;

		public readonly SupportedType DataType;

		private readonly byte[] m_buffer;

		private FileStream m_fileStream;

		public GridTileSource(string path, int tileSizeX, int tileSizeY, int tilesX, int tilesY)
		{
			FilePath = path;

			TilesX = tilesX;
			TilesY = tilesY;
			TileSizeX = tileSizeX;
			TileSizeY = tileSizeY;
			TileSizePixels = TileSizeX * TileSizeY;

			SizeX = TilesX * TileSizeX;
			SizeY = TilesY * TileSizeY;
			SizePixels = (long)SizeX * SizeY;

			DataType = SupportedType.GetType<T>();

			TileSizeBytes = TileSizePixels * DataType.Size;
			SizeBytes = (long)SizePixels * DataType.Size;

			m_buffer = new byte[TileSizeBytes];

			Allocate();
		}

		private void Allocate()
		{
			using (FileStream outputStream = File.OpenWrite(FilePath))
			{
				outputStream.SetLength(SizeBytes);
			}
		}

		public void WriteTile(int tileX, int tileY, T[,] buffer)
		{
			// verify buffer size?

			Open(true);
			Seek(tileX, tileY);

			Copy(buffer, m_buffer);
			m_fileStream.Write(m_buffer, 0, TileSizeBytes);
		}

		public void ReadTile(int tileX, int tileY, T[,] buffer)
		{
			// verify buffer size?

			Open(false);
			Seek(tileX, tileY);

			m_fileStream.Read(m_buffer, 0, TileSizeBytes);
			Copy(m_buffer, buffer);
		}

		public void Seek(int tileX, int tileY)
		{
			long position = (long)tileX * TilesY + tileY;
			if (m_fileStream.Position != position)
			{
				m_fileStream.Seek(position, SeekOrigin.Begin);

				if (position < m_fileStream.Position)
					Context.WriteLine("bad seek");
			}
		}

		private unsafe void Copy(T[,] src, byte[] dst)
		{
			fixed (byte* d = dst)
			{
				switch (DataType.TypeCode)
				{
					case TypeCode.Byte:   { byte[,]   s = src as byte[,];   byte*   p = (byte*)d;   for (int x = 0; x < TileSizeX; x++) { for (int y = 0; y < TileSizeY; y++) { *p = s[x, y]; ++p; } } }; break;
					case TypeCode.SByte:  { sbyte[,]  s = src as sbyte[,];  sbyte*  p = (sbyte*)d;  for (int x = 0; x < TileSizeX; x++) { for (int y = 0; y < TileSizeY; y++) { *p = s[x, y]; ++p; } } }; break;
					case TypeCode.Int16:  { short[,]  s = src as short[,];  short*  p = (short*)d;  for (int x = 0; x < TileSizeX; x++) { for (int y = 0; y < TileSizeY; y++) { *p = s[x, y]; ++p; } } }; break;
					case TypeCode.UInt16: { ushort[,] s = src as ushort[,]; ushort* p = (ushort*)d; for (int x = 0; x < TileSizeX; x++) { for (int y = 0; y < TileSizeY; y++) { *p = s[x, y]; ++p; } } }; break;
					case TypeCode.Int32:  { int[,]    s = src as int[,];    int*    p = (int*)d;    for (int x = 0; x < TileSizeX; x++) { for (int y = 0; y < TileSizeY; y++) { *p = s[x, y]; ++p; } } }; break;
					case TypeCode.UInt32: { uint[,]   s = src as uint[,];   uint*   p = (uint*)d;   for (int x = 0; x < TileSizeX; x++) { for (int y = 0; y < TileSizeY; y++) { *p = s[x, y]; ++p; } } }; break;
					case TypeCode.Int64:  { long[,]   s = src as long[,];   long*   p = (long*)d;   for (int x = 0; x < TileSizeX; x++) { for (int y = 0; y < TileSizeY; y++) { *p = s[x, y]; ++p; } } }; break;
					case TypeCode.UInt64: { ulong[,]  s = src as ulong[,];  ulong*  p = (ulong*)d;  for (int x = 0; x < TileSizeX; x++) { for (int y = 0; y < TileSizeY; y++) { *p = s[x, y]; ++p; } } }; break;
					case TypeCode.Single: { float[,]  s = src as float[,];  float*  p = (float*)d;  for (int x = 0; x < TileSizeX; x++) { for (int y = 0; y < TileSizeY; y++) { *p = s[x, y]; ++p; } } }; break;
					case TypeCode.Double: { double[,] s = src as double[,]; double* p = (double*)d; for (int x = 0; x < TileSizeX; x++) { for (int y = 0; y < TileSizeY; y++) { *p = s[x, y]; ++p; } } }; break;

					default:
						throw new NotSupportedException();
				}
			}
		}

		private unsafe void Copy(byte[] src, T[,] dst)
		{
			fixed (byte* s = src)
			{
				switch (DataType.TypeCode)
				{
					case TypeCode.Byte:   { byte[,]   d = dst as byte[,];   byte*   p = (byte*)s;   for (int x = 0; x < TileSizeX; x++) { for (int y = 0; y < TileSizeY; y++) { d[x, y] = *p; ++p; } } }; break;
					case TypeCode.SByte:  { sbyte[,]  d = dst as sbyte[,];  sbyte*  p = (sbyte*)s;  for (int x = 0; x < TileSizeX; x++) { for (int y = 0; y < TileSizeY; y++) { d[x, y] = *p; ++p; } } }; break;
					case TypeCode.Int16:  { short[,]  d = dst as short[,];  short*  p = (short*)s;  for (int x = 0; x < TileSizeX; x++) { for (int y = 0; y < TileSizeY; y++) { d[x, y] = *p; ++p; } } }; break;
					case TypeCode.UInt16: { ushort[,] d = dst as ushort[,]; ushort* p = (ushort*)s; for (int x = 0; x < TileSizeX; x++) { for (int y = 0; y < TileSizeY; y++) { d[x, y] = *p; ++p; } } }; break;
					case TypeCode.Int32:  { int[,]    d = dst as int[,];    int*    p = (int*)s;    for (int x = 0; x < TileSizeX; x++) { for (int y = 0; y < TileSizeY; y++) { d[x, y] = *p; ++p; } } }; break;
					case TypeCode.UInt32: { uint[,]   d = dst as uint[,];   uint*   p = (uint*)s;   for (int x = 0; x < TileSizeX; x++) { for (int y = 0; y < TileSizeY; y++) { d[x, y] = *p; ++p; } } }; break;
					case TypeCode.Int64:  { long[,]   d = dst as long[,];   long*   p = (long*)s;   for (int x = 0; x < TileSizeX; x++) { for (int y = 0; y < TileSizeY; y++) { d[x, y] = *p; ++p; } } }; break;
					case TypeCode.UInt64: { ulong[,]  d = dst as ulong[,];  ulong*  p = (ulong*)s;  for (int x = 0; x < TileSizeX; x++) { for (int y = 0; y < TileSizeY; y++) { d[x, y] = *p; ++p; } } }; break;
					case TypeCode.Single: { float[,]  d = dst as float[,];  float*  p = (float*)s;  for (int x = 0; x < TileSizeX; x++) { for (int y = 0; y < TileSizeY; y++) { d[x, y] = *p; ++p; } } }; break;
					case TypeCode.Double: { double[,] d = dst as double[,]; double* p = (double*)s; for (int x = 0; x < TileSizeX; x++) { for (int y = 0; y < TileSizeY; y++) { d[x, y] = *p; ++p; } } }; break;

					default:
						throw new NotSupportedException();
				}
			}
		}

		public void Open(bool forWriting)
		{
			if (m_fileStream != null)
			{
				if (forWriting && !m_fileStream.CanWrite)
				{
					Close();
				}
				else if (!forWriting && !m_fileStream.CanRead)
				{
					Close();
				}
			}

			if (m_fileStream == null)
			{
				if (forWriting)
					m_fileStream = new FileStream(FilePath, FileMode.Open, FileAccess.Write, FileShare.None, BufferManager.BUFFER_SIZE_BYTES);
				else
					m_fileStream = new FileStream(FilePath, FileMode.Open, FileAccess.Read, FileShare.None, BufferManager.BUFFER_SIZE_BYTES, FileOptions.RandomAccess);
			}
		}

		public void Close()
		{
			if (m_fileStream != null)
			{
				m_fileStream.Dispose();
				m_fileStream = null;
			}
		}

		#region IDisposable Members

		public void Dispose()
		{
			Close();
		}

		#endregion
	}
}
