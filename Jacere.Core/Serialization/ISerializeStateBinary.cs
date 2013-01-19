using System.IO;

namespace Jacere.Core
{
	public interface ISerializeStateBinary : ISerializeBinary
	{
		string GetIdentifier();
		void Deserialize(BinaryReader reader);
	}
}
