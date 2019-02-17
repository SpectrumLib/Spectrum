using System;
using System.Collections.Generic;

namespace Spectrum.Content
{
	// Contains a cache of the loaded ContentLoader types, and facilitates creating instances of the types
	//   for use in content packs and streams
	internal static class LoaderCache
	{
		#region Fields
		private static readonly Dictionary<string, LoaderType> s_typeCache;
		#endregion // Fields

		// Registers the builtin loader types, all new "vanilla" loader types must be added here to be registered
		static LoaderCache()
		{
			s_typeCache = new Dictionary<string, LoaderType>();
		}
	}
}
