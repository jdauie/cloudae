using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;

using Jacere.Core.Geometry;
using Jacere.Data.PointCloud.Handlers;

namespace Jacere.Data.PointCloud
{
	/// <summary>
	/// Format1 = [Format0][GPSTime]
	/// </summary>
	[StructLayout(LayoutKind.Sequential)]
	public struct LASPointFormat1
	{
		private LASPointFormat0 m_base;
		private double m_gpsTime;

		#region Properties

		public int X { get { return m_base.X; } }
		public int Y { get { return m_base.Y; } }
		public int Z { get { return m_base.Z; } }
		public ushort Intensity { get { return m_base.Intensity; } }

		public byte ReturnNumber { get { return m_base.ReturnNumber; } }
		public byte NumReturns { get { return m_base.NumReturns; } }
		public byte ScanDirection { get { return m_base.ScanDirection; } }
		public byte EdgeOfFlightLine { get { return m_base.EdgeOfFlightLine; } }

		public byte Classification { get { return m_base.Classification; } }
		public sbyte ScanAngleRank { get { return m_base.ScanAngleRank; } }
		public byte UserData { get { return m_base.UserData; } }
		public ushort PointSourceID { get { return m_base.PointSourceID; } }

		public double GPSTime { get { return m_gpsTime; } }

		#endregion
	}

	/// <summary>
	/// Format2 = [Format0][RGB]
	/// </summary>
	[StructLayout(LayoutKind.Sequential)]
	public struct LASPointFormat2
	{
		private LASPointFormat0 m_base;
		private LASPointFormat_RGB m_rgb;

		#region Properties

		public int X { get { return m_base.X; } }
		public int Y { get { return m_base.Y; } }
		public int Z { get { return m_base.Z; } }
		public ushort Intensity { get { return m_base.Intensity; } }

		public byte ReturnNumber { get { return m_base.ReturnNumber; } }
		public byte NumReturns { get { return m_base.NumReturns; } }
		public byte ScanDirection { get { return m_base.ScanDirection; } }
		public byte EdgeOfFlightLine { get { return m_base.EdgeOfFlightLine; } }

		public byte Classification { get { return m_base.Classification; } }
		public sbyte ScanAngleRank { get { return m_base.ScanAngleRank; } }
		public byte UserData { get { return m_base.UserData; } }
		public ushort PointSourceID { get { return m_base.PointSourceID; } }

		public ushort Red { get { return m_rgb.Red; } }
		public ushort Green { get { return m_rgb.Green; } }
		public ushort Blue { get { return m_rgb.Blue; } }

		#endregion
	}

	/// <summary>
	/// Format3 = [Format1][RGB].
	/// </summary>
	[StructLayout(LayoutKind.Sequential)]
	public struct LASPointFormat3
	{
		private LASPointFormat1 m_base;
		private LASPointFormat_RGB m_rgb;

		#region Properties

		public int X { get { return m_base.X; } }
		public int Y { get { return m_base.Y; } }
		public int Z { get { return m_base.Z; } }
		public ushort Intensity { get { return m_base.Intensity; } }

		public byte ReturnNumber { get { return m_base.ReturnNumber; } }
		public byte NumReturns { get { return m_base.NumReturns; } }
		public byte ScanDirection { get { return m_base.ScanDirection; } }
		public byte EdgeOfFlightLine { get { return m_base.EdgeOfFlightLine; } }

		public byte Classification { get { return m_base.Classification; } }
		public sbyte ScanAngleRank { get { return m_base.ScanAngleRank; } }
		public byte UserData { get { return m_base.UserData; } }
		public ushort PointSourceID { get { return m_base.PointSourceID; } }

		public double GPSTime { get { return m_base.GPSTime; } }

		public ushort Red { get { return m_rgb.Red; } }
		public ushort Green { get { return m_rgb.Green; } }
		public ushort Blue { get { return m_rgb.Blue; } }

		#endregion
	}

	/// <summary>
	/// Format4 = [Format1][WavePackets].
	/// </summary>
	[StructLayout(LayoutKind.Sequential)]
	public struct LASPointFormat4
	{
		private LASPointFormat1 m_base;
		private LASPointFormat_WavePackets m_wave;

		#region Properties

		public int X { get { return m_base.X; } }
		public int Y { get { return m_base.Y; } }
		public int Z { get { return m_base.Z; } }
		public ushort Intensity { get { return m_base.Intensity; } }

		public byte ReturnNumber { get { return m_base.ReturnNumber; } }
		public byte NumReturns { get { return m_base.NumReturns; } }
		public byte ScanDirection { get { return m_base.ScanDirection; } }
		public byte EdgeOfFlightLine { get { return m_base.EdgeOfFlightLine; } }

		public byte Classification { get { return m_base.Classification; } }
		public sbyte ScanAngleRank { get { return m_base.ScanAngleRank; } }
		public byte UserData { get { return m_base.UserData; } }
		public ushort PointSourceID { get { return m_base.PointSourceID; } }

		public double GPSTime { get { return m_base.GPSTime; } }

		public ushort WavePacketDescriptorIndex { get { return m_wave.WavePacketDescriptorIndex; } }
		public ulong OffsetToWaveformData { get { return m_wave.OffsetToWaveformData; } }
		public uint WaveformPacketSize { get { return m_wave.WaveformPacketSize; } }
		public float ReturnPointWaveformLocation { get { return m_wave.ReturnPointWaveformLocation; } }
		public float Xt { get { return m_wave.Xt; } }
		public float Yt { get { return m_wave.Yt; } }
		public float Zt { get { return m_wave.Zt; } }

		#endregion
	}

	/// <summary>
	/// Format5 = [Format3][WavePackets].
	/// </summary>
	[StructLayout(LayoutKind.Sequential)]
	public struct LASPointFormat5
	{
		private LASPointFormat3 m_base;
		private LASPointFormat_WavePackets m_wave;

		#region Properties

		public int X { get { return m_base.X; } }
		public int Y { get { return m_base.Y; } }
		public int Z { get { return m_base.Z; } }
		public ushort Intensity { get { return m_base.Intensity; } }

		public byte ReturnNumber { get { return m_base.ReturnNumber; } }
		public byte NumReturns { get { return m_base.NumReturns; } }
		public byte ScanDirection { get { return m_base.ScanDirection; } }
		public byte EdgeOfFlightLine { get { return m_base.EdgeOfFlightLine; } }

		public byte Classification { get { return m_base.Classification; } }
		public sbyte ScanAngleRank { get { return m_base.ScanAngleRank; } }
		public byte UserData { get { return m_base.UserData; } }
		public ushort PointSourceID { get { return m_base.PointSourceID; } }

		public double GPSTime { get { return m_base.GPSTime; } }

		public ushort Red { get { return m_base.Red; } }
		public ushort Green { get { return m_base.Green; } }
		public ushort Blue { get { return m_base.Blue; } }

		public ushort WavePacketDescriptorIndex { get { return m_wave.WavePacketDescriptorIndex; } }
		public ulong OffsetToWaveformData { get { return m_wave.OffsetToWaveformData; } }
		public uint WaveformPacketSize { get { return m_wave.WaveformPacketSize; } }
		public float ReturnPointWaveformLocation { get { return m_wave.ReturnPointWaveformLocation; } }
		public float Xt { get { return m_wave.Xt; } }
		public float Yt { get { return m_wave.Yt; } }
		public float Zt { get { return m_wave.Zt; } }

		#endregion
	}
}
