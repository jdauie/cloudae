using System;
using System.Collections.Generic;
using Jacere.Core;

namespace Jacere.Core
{
	public interface ISourcePaths
	{
		IEnumerable<string> SourcePaths { get; }
	}
}
