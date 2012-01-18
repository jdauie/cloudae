using System.IO;

namespace CloudAE.Core
{
	public interface ISerializeBinary
	{
		void Serialize(BinaryWriter writer);
	}
}
