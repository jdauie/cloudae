using System;

namespace Jacere.Core
{
	public class PropertyName : IEquatable<PropertyName>
	{
		public readonly string ID;
		public readonly string Path;
		public readonly string Name;

		public PropertyName(string id, string path, string name)
		{
			ID  = id;
			Path = path;
			Name = name;
		}

		public override int GetHashCode()
		{
			return ID.GetHashCode();
		}

		public override bool Equals(object obj)
		{
			return Equals(obj as PropertyName);
		}

		public bool Equals(PropertyName other)
		{
			return (other != null && ID.Equals(other.ID, StringComparison.OrdinalIgnoreCase));
		}

		public override string ToString()
		{
			return ID;
		}
	}
}
