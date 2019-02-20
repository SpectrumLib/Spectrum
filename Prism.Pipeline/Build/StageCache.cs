using System;
using System.Collections.Generic;
using System.Reflection;
using Prism.Builtin;

namespace Prism.Build
{
	// Holds type info for importers and processors that the build pipeline can use
	internal class StageCache
	{
		#region Fields
		public readonly BuildEngine Engine;

		private readonly Dictionary<string, ImporterType> _importers;
		public IReadOnlyDictionary<string, ImporterType> Importers => _importers;

		private readonly Dictionary<string, ProcessorType> _processors;
		public IReadOnlyDictionary<string, ProcessorType> Processors => _processors;
		#endregion // Fields

		public StageCache(BuildEngine engine)
		{
			Engine = engine;
			_importers = new Dictionary<string, ImporterType>();
			_processors = new Dictionary<string, ProcessorType>();

			// Add the builtin importers
			// TODO: This list must be updated whenever new builtin stages are added, or else they wont appear
			_importers.Add(nameof(PassthroughImporter), ImporterType.TryCreate(engine, typeof(PassthroughImporter)));
			_importers.Add(nameof(TextureImporter), ImporterType.TryCreate(engine, typeof(TextureImporter)));

			// Add the builtin processors
			// TODO: This list must be updated whenever new builtin stages are added, or else they wont appear
			_processors.Add(nameof(PassthroughProcessor), ProcessorType.TryCreate(engine, typeof(PassthroughProcessor)));
			_processors.Add(nameof(TextureProcessor), ProcessorType.TryCreate(engine, typeof(TextureProcessor)));
		}

		public void AddAssemblyTypes(Assembly asm)
		{
			ImporterType itype = null;
			ProcessorType ptype = null;
			foreach (var type in asm.GetTypes())
			{
				itype = ImporterType.TryCreate(Engine, type);
				if (itype != null)
					_importers.Add(itype.TypeName, itype);
				else
				{
					ptype = ProcessorType.TryCreate(Engine, type);
					if (ptype != null)
						_processors.Add(ptype.TypeName, ptype);
				}
			}
		}
	}
}
