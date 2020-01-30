/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2018-2020 The Spectrum Team
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Prism.Pipeline
{
	// Contains a cache of ContentProcessor types that are available to a BuildEngine
	internal sealed class ProcessorTypeCache
	{
		#region Fields
		public readonly BuildEngine Engine;
		public BuildLogger Logger => Engine.Logger;

		private readonly List<string> _loadedAssemblies = new List<string>(); // Prevent double-loading

		private readonly List<ProcessorType> _procTypes = new List<ProcessorType>();
		public IReadOnlyList<ProcessorType> ProcTypes => _procTypes;

		public ProcessorType NoneType { get; private set; } = null;
		#endregion // Fields

		public ProcessorTypeCache(BuildEngine engine)
		{
			Engine = engine;
		}

		// Gets the processor type assoicated with the content type name, or null
		public ProcessorType FindContentType(string ctype) =>
			_procTypes.FirstOrDefault(pt => pt.Attr.ContentType == ctype);

		// Gets the processor type associated with the default extension, or null
		public ProcessorType FindExtension(string ext) =>
			_procTypes.FirstOrDefault(pt => pt.Attr.Extensions.Contains(ext));

		// Gets the processor type with the display name, or null
		public ProcessorType FindDisplayName(string name) =>
			_procTypes.FirstOrDefault(pt => pt.Attr.DisplayName == name);

		// Searches the assembly for public types that are decorated with ContentProcessorAttribute, and are
		//    subclasses of ContentProcessor, and adds them to the cache
		public bool LoadProcessors(Assembly assembly)
		{
			if (_loadedAssemblies.Contains(assembly.FullName))
				return true;

			// Filter a list of types that have the super class and attribute, and are instantiable
			var types = assembly.GetTypes()
								.Where(typ => typ.GetCustomAttribute(ContentProcessorAttribute.TYPE) != null)
								.Where(typ => typ.IsSubclassOf(ContentProcessor.TYPE))
								.Where(typ => !typ.IsAbstract);

			// Iterate over the filtered types
			foreach (var proctype in types)
			{
				// Get the attribute
				var attr = proctype.GetCustomAttribute(ContentProcessorAttribute.TYPE) as ContentProcessorAttribute;

				// Check for no-args constructor and non-generic
				if (proctype.IsGenericType)
				{
					Logger.EngineWarn($"Ignoring generic content processor '{attr.DisplayName}' ({proctype.Name}).");
					continue;
				}
				var ctor = proctype.GetConstructor(Type.EmptyTypes);
				if (ctor == null)
				{
					Logger.EngineWarn($"Ignoring bad constructor content processor '{attr.DisplayName}' ({proctype.Name})");
					continue;
				}

				// Check for double-registration for names, types, and extensions
				foreach (var tinfo in _procTypes)
				{
					if (tinfo.Attr.DisplayName == attr.DisplayName)
					{
						Logger.EngineError($"Duplicate content processor display name '{attr.DisplayName}' - " +
							$"between '{tinfo.Type.Name}' and '{proctype.Name}'.");
						return false;
					}
					if (tinfo.Attr.ContentType == attr.ContentType)
					{
						Logger.EngineError($"Duplicate content type ('{attr.ContentType}') registration - " +
							$"between '{tinfo.Type.Name}' and '{proctype.Name}'.");
						return false;
					}
					var extUn = tinfo.Attr.Extensions.Intersect(attr.Extensions);
					if (extUn.Any())
					{
						Logger.EngineWarn($"Duplicate default extension(s) '{String.Join(',', extUn)}' - " +
							$"default to first registered ({tinfo.Type.Name}).");
					}
				}

				// Add the processor type (and set as none type if applicable)
				_procTypes.Add(new ProcessorType(proctype, attr, ctor));
				if ((NoneType == null) && (attr.ContentType == "None"))
					NoneType = _procTypes[^1];
			}

			_loadedAssemblies.Add(assembly.FullName);
			return true;
		}
	}

	// Contains a record of a ContentProcessor type, and associated metadata objects
	internal class ProcessorType
	{
		#region Fields
		public readonly Type Type;
		public readonly ContentProcessorAttribute Attr;
		public readonly ConstructorInfo Ctor;
		#endregion // Fields

		public ProcessorType(Type type, ContentProcessorAttribute attr, ConstructorInfo ctor)
		{
			Type = type;
			Attr = attr;
			Ctor = ctor;
		}
	}
}
