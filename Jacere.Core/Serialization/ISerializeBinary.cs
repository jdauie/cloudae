using System.IO;

namespace Jacere.Core
{
	public interface ISerializeBinary
	{
		void Serialize(BinaryWriter writer);
	}
}
