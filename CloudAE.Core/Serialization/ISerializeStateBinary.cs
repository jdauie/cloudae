using System.IO;

namespace CloudAE.Core
{
	public interface ISerializeStateBinary : ISerializeBinary
	{
		string GetIdentifier();
		void Deserialize(BinaryReader reader);
	}
}
