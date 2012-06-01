using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using CloudAE.Core.Handlers;

namespace CloudAE.Core.Geometry
{
	/// <summary>
	/// Point Data Record Format 0 contains the core 20 bytes 
	/// that are shared by Point Data Record Formats 0 to 5.
	/// </summary>
	[StructLayout(LayoutKind.Sequential)]
	public struct LASPointFormat0 : IQuantizedPoint3D
	{
		private int m_x;
		private int m_y;
		private int m_z;
		private ushort m_intensity;
		private byte m_options;
		private byte m_classifications;
		private sbyte m_scanAngleRank;
		private byte m_userData;
		private ushort m_pointSourceID;

		#region Properties

		public int X { get { return m_x; } }
		public int Y { get { return m_y; } }
		public int Z { get { return m_z; } }
		public ushort Intensity { get { return m_intensity; } }
		public byte Options { get { return m_options; } }
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
}
