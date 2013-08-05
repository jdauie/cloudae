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
	/// Point Data Record Format 6 contains the core 30 bytes that are shared by Point Data Record Formats 6 to 10.
	/// </summary>
	[StructLayout(LayoutKind.Sequential)]
	public struct LASPointFormat6
	{
		private LASPointFormat_XYZ m_xyz;
		private ushort m_intensity;

		//Return Number
		//4 bits
		//Number of Returns (given pulse)
		//4 bits

		//ClassificationFlags
		//4 bits
		//Scanner Channel
		//2 bits
		//Scan Direction Flag
		//1 bit
		//Edge of Flight Line
		//1 bit

		//Classification
		//1 byte
		
		private byte m_userData;
		private short m_scanAngle;
		private ushort m_pointSourceID;
		private double m_gpsTime;

		#region Properties

		

		public double GPSTime { get { return m_gpsTime; } }

		#endregion
	}
}
