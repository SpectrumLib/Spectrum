﻿/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2018-2019 The Spectrum Team
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */
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

        private static readonly Dictionary<string, LoaderType> _TypeCache;

		// Gets the type from the cache, or tries to load it
		public static LoaderType GetOrLoad(string name)
		{
			if (_TypeCache.ContainsKey(name))
				return _TypeCache[name];

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
					var ltype = LoaderType.TryCreate(type, parts[0], tName, out var error);
					if (ltype != null)
					{
						_TypeCache.Add(name, ltype);
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
			_TypeCache = new Dictionary<string, LoaderType>();

			_TypeCache.Add("Spectrum:PassthroughLoader", new LoaderType(typeof(PassthroughLoader), "Spectrum", "PassthroughLoader"));
			//s_typeCache.Add("Spectrum:TextureLoader", new LoaderType(typeof(TextureLoader), "Spectrum", "TextureLoader"));
			//s_typeCache.Add("Spectrum:AudioLoader", new LoaderType(typeof(AudioLoader), "Spectrum", "AudioLoader"));
		}
	}
}
