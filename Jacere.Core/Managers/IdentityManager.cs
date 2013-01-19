using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Jacere.Core
{
	public static class IdentityManager
	{
#warning this list is just for debugging reasons at this point
		private static List<Identity> m_identities;

		private static uint m_nextIncrementalValue;

		static IdentityManager()
		{
			m_identities = new List<Identity>();

			m_nextIncrementalValue = 1;
		}

		public static Identity AcquireIdentity(string name)
		{
			return AcquireIdentity(name, IdentityType.Unknown);
		}

		public static Identity AcquireIdentity(string name, IdentityType type)
		{
			Identity id = new Identity(m_nextIncrementalValue, name, type);
			m_identities.Add(id);

			++m_nextIncrementalValue;

			return id;
		}
	}
}
