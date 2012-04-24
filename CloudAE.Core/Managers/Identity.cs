using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CloudAE.Core
{
	public enum IdentityType
	{
		Unknown = 0,
		Process
	}

	public class Identity
	{
		private readonly uint m_id;
		private readonly string m_name;
		private readonly IdentityType m_type;

		public uint ID
		{
			get { return m_id; }
		}

		public string Name
		{
			get { return m_name; }
		}

		public IdentityType Type
		{
			get { return m_type; }
		}

		public Identity(uint id, string name, IdentityType type)
		{
			m_id = id;
			m_name = name;
			m_type = type;
		}

		public override string ToString()
		{
			return string.Format("[{2}] {0}: {1}", m_id, m_name, m_type);
		}
	}
}
