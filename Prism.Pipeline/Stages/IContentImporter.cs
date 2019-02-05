using System;
using System.IO;

namespace Prism
{
	// Base non-generic handle type for working with ContentImporter instances
	internal interface IContentImporter
	{
		Type OutputType { get; }

		object Import(FileStream stream, ImporterContext ctx);
	}
}
