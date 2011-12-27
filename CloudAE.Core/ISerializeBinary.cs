using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace CloudAE.Core
{
	public interface ISerializeBinary
	{
		void Serialize(BinaryWriter writer);
	}

	public interface ISerializeStateBinary : ISerializeBinary
	{
		string GetIdentifier();
		void Deserialize(BinaryReader reader);
	}
}
