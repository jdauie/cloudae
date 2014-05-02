using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;

using Jacere.Core.Geometry;
using Jacere.Data.PointCloud.Handlers;

namespace Jacere.Data.PointCloud
{
	public enum LASPointFormat0_Classification : byte
	{
		NeverClassified = 0,
		Unclassified,
		Ground,
		LowVegetation,
		MediumVegetation,
		HighVegetation,
		Building,
		LowPoint,
		ModelKeyPoint,
		Water,
		// 10-11 reserved
		OverlapPoints = 12
		// 13-31 reserved
	}

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
	public struct LASPointFormat_Classification
	{
		private byte m_classification;

		#region Properties

		public byte Classification { get { return (byte)(m_classification & ((1 << 5) - 1)); } }
		public bool Synthetic { get { return ((m_classification >> 5) & 1) == 1; } }
		public bool KeyPoint { get { return ((m_classification >> 6) & 1) == 1; } }
		public bool Withheld { get { return ((m_classification >> 7) & 1) == 1; } }

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
		private LASPointFormat_Classification m_classification;
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

		public byte Classification { get { return m_classification.Classification; } }
		public sbyte ScanAngleRank { get { return m_scanAngleRank; } }
		public byte UserData { get { return m_userData; } }
		public ushort PointSourceID { get { return m_pointSourceID; } }

		#endregion

		public void Create()
		{
			var attributeSet = new LASPointAttributeSet();
			attributeSet.Add(new LASPointAttribute<LASPointFormat_XYZ>());
			attributeSet.Add(new LASPointAttribute<ushort>());
			attributeSet.Add(new LASPointAttribute<LASPointFormat_Options>());
			attributeSet.Add(new LASPointAttribute<LASPointFormat_Classification>());
			attributeSet.Add(new LASPointAttribute<sbyte>());
			attributeSet.Add(new LASPointAttribute<byte>());
			attributeSet.Add(new LASPointAttribute<ushort>());
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
}
