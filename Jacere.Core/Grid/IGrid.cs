using System;

namespace Jacere.Core
{
	public interface IGrid
	{
		ushort SizeX { get; }
		ushort SizeY { get; }
	}

	public interface IGridDefinition : IGrid
	{
		GridDefinition Def { get; }
	}
}
