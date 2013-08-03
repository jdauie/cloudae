using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;

using Jacere.Core.Geometry;
using Jacere.Data.PointCloud.Handlers;

namespace Jacere.Data.PointCloud
{
	[StructLayout(LayoutKind.Sequential)]
	public struct LASPointFormat_XYZ
	{
		private int m_x;
		private int m_y;
		private int m_z;

		#region Properties

		public int X { get { return m_x; } }
		public int Y { get { return m_y; } }
		public int Z { get { return m_z; } }

		#endregion
	}

	[StructLayout(LayoutKind.Sequential)]
	public struct LASPointFormat_Options
	{
		private byte m_options;

		#region Properties

		public byte ReturnNumber { get { return (byte)(m_options & ((1 << 3) - 1)); } }
		public byte NumReturns { get { return (byte)((m_options >> 3) & ((1 << 3) - 1)); } }
		public byte ScanDirection { get { return (byte)((m_options >> 6) & 1); } }
		public byte EdgeOfFlightLine { get { return (byte)(m_options >> 7); } }

		#endregion
	}

	[StructLayout(LayoutKind.Sequential)]
	public struct LASPointFormat_RGB
	{
		private ushort m_red;
		private ushort m_green;
		private ushort m_blue;

		#region Properties

		public ushort Red { get { return m_red; } }
		public ushort Green { get { return m_green; } }
		public ushort Blue { get { return m_blue; } }

		#endregion
	}

	[StructLayout(LayoutKind.Sequential)]
	public struct LASPointFormat_WavePackets
	{
		private byte m_wavePacketDescriptorIndex;
		private ulong m_offsetToWaveformData;
		private uint m_waveformPacketSize;
		private float m_returnPointWaveformLocation;
		private float m_xt;
		private float m_yt;
		private float m_zt;

		#region Properties

		public ushort WavePacketDescriptorIndex { get { return m_wavePacketDescriptorIndex; } }
		public ulong OffsetToWaveformData { get { return m_offsetToWaveformData; } }
		public uint WaveformPacketSize { get { return m_waveformPacketSize; } }
		public float ReturnPointWaveformLocation { get { return m_returnPointWaveformLocation; } }
		public float Xt { get { return m_xt; } }
		public float Yt { get { return m_yt; } }
		public float Zt { get { return m_zt; } }

		#endregion
	}

	/// <summary>
	/// Point Data Record Format 0 contains the core 20 bytes that are shared by Point Data Record Formats 0 to 5.
	/// </summary>
	[StructLayout(LayoutKind.Sequential)]
	public struct LASPointFormat0// : IQuantizedPoint3D
	{
		private LASPointFormat_XYZ m_xyz;
		private ushort m_intensity;
		private LASPointFormat_Options m_options;
		private byte m_classifications;
		private sbyte m_scanAngleRank;
		private byte m_userData;
		private ushort m_pointSourceID;

		#region Properties

		public int X { get { return m_xyz.X; } }
		public int Y { get { return m_xyz.Y; } }
		public int Z { get { return m_xyz.Z; } }
		public ushort Intensity { get { return m_intensity; } }

		public byte ReturnNumber { get { return m_options.ReturnNumber; } }
		public byte NumReturns { get { return m_options.NumReturns; } }
		public byte ScanDirection { get { return m_options.ScanDirection; } }
		public byte EdgeOfFlightLine { get { return m_options.EdgeOfFlightLine; } }

		public byte Classifications { get { return m_classifications; } }
		public sbyte ScanAngleRank { get { return m_scanAngleRank; } }
		public byte UserData { get { return m_userData; } }
		public ushort PointSourceID { get { return m_pointSourceID; } }

		#endregion

		public void Create()
		{
			var attributeSet = new LASPointAttributeSet();
			attributeSet.Add(new LASPointAttribute<uint>());
		}

		/// <summary>
		/// Returns a <see cref="System.String"/> that represents this instance.
		/// </summary>
		/// <returns>
		/// A <see cref="System.String"/> that represents this instance.
		/// </returns>
		public override string ToString()
		{
			return String.Format("({0}, {1}, {2})", X, Y, Z);
		}
	}

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

		public byte Classifications { get { return m_base.Classifications; } }
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

		public byte Classifications { get { return m_base.Classifications; } }
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

		public byte Classifications { get { return m_base.Classifications; } }
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

		public byte Classifications { get { return m_base.Classifications; } }
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

		public byte Classifications { get { return m_base.Classifications; } }
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
