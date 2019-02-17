using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace Spectrum.Content
{
	// Contains a cache of the loaded ContentLoader types, and facilitates creating instances of the types
	//   for use in content packs and streams
	internal static class LoaderCache
	{
		private static readonly char[] COLON_SPLIT = { ':' };

		#region Fields
		private static readonly Dictionary<string, LoaderType> s_typeCache;
		#endregion // Fields

		// Gets the type from the cache, or tries to load it
		public static LoaderType GetOrLoad(string name)
		{
			if (s_typeCache.ContainsKey(name))
				return s_typeCache[name];

			// Get the name components
			var parts = name.Split(COLON_SPLIT, 2, StringSplitOptions.RemoveEmptyEntries);
			if (parts.Length != 2)
				throw new ContentException($"The content loader name '{name}' is not a valid format.");
			var aPath = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), $"{parts[0]}.dll"));
			var tName = parts[1];

			// Open the assembly
			if (!File.Exists(aPath))
				throw new ContentException($"Cannot load the assembly '{aPath}', the file does not exist.");
			Assembly asm = null;
			try
			{
				asm = Assembly.LoadFile(aPath);
			}
			catch (Exception e)
			{
				throw new ContentException($"Could not load the assembly '{aPath}', reason: {e.Message}", e);
			}

			// Try to find and load the type
			try
			{
				foreach (var type in asm.GetExportedTypes())
				{
					var ltype = LoaderType.TryCreate(type, tName, out var error);
					if (ltype != null)
					{
						s_typeCache.Add(name, ltype);
						return ltype;
					}
					else if (error != null)
						throw new ContentException($"Could not load the ContentLoader type '{name}', reason: {error}");
				}
			}
			catch (ContentException) { throw; }
			catch (Exception e)
			{
				throw new ContentException($"Unable to load content types for assembly '{aPath}', reason: {e.Message}", e);
			}

			// Couldn't find the type in the assembly
			throw new ContentException($"Could not find the ContentLoader type '{tName}' in the assembly '{aPath}'.");
		}

		// Registers the builtin loader types, all new "vanilla" loader types must be added here to be registered
		static LoaderCache()
		{
			s_typeCache = new Dictionary<string, LoaderType>();
		}
	}
}
