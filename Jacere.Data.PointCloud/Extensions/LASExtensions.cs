using System;
using System.IO;

namespace Jacere.Data.PointCloud
{
	public static class LASExtensions
	{
		public static LASProjectID ReadLASProjectID(this BinaryReader reader)
		{
			return new LASProjectID(reader);
		}

		public static LASVersionInfo ReadLASVersionInfo(this BinaryReader reader)
		{
			return new LASVersionInfo(reader);
		}

		public static LASGlobalEncoding ReadLASGlobalEncoding(this BinaryReader reader)
		{
			return new LASGlobalEncoding(reader);
		}

		public static LASVLR ReadLASVLR(this BinaryReader reader)
		{
			return new LASVLR(reader);
		}

		public static LASEVLR ReadLASEVLR(this BinaryReader reader)
		{
			return new LASEVLR(reader);
		}

		public static LASHeader ReadLASHeader(this BinaryReader reader)
		{
			return new LASHeader(reader);
		}
	}
}
