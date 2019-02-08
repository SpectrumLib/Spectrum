﻿using System;
using System.Collections.Generic;
using Prism.Content;

namespace Prism.Build
{
	// Holds the instance of a ContentImporter type used by a BuildTask
	internal class ImporterInstance
	{
		public readonly ImporterType Type;
		public readonly IContentImporter Instance;

		public ImporterInstance(ImporterType type)
		{
			Type = type;
			Instance = type.CreateInstance();
		}
	}

	// Holds the instance of a ContentProcessor and ContentWriter type used by a BuildTask
	internal class ProcessorInstance
	{
		public readonly ProcessorType Type;
		public readonly IContentProcessor Instance;
		public readonly IContentWriter WriterInstance;

		public ProcessorInstance(ProcessorType type)
		{
			Type = type;
			Instance = type.CreateInstance();
			WriterInstance = type.CreateWriterInstance();
		}

		// Resets all settable fields back to default, and sets the ones that are included in the item args
		public void UpdateFields(BuildEngine engine, ContentItem item, uint id, List<(string Key, string Value)> args)
		{
			foreach (var field in Type.Fields)
			{
				var idx = args.FindIndex(arg => arg.Key == field.ParamName);
				if (idx == -1)
					field.Info.SetValue(Instance, field.DefaultValue);
				else
				{
					try
					{
						var val = field.Converter.ConvertFromString(args[idx].Value);
						field.Info.SetValue(Instance, val);
					}
					catch
					{
						engine.Logger.EngineError($"The content item '{item.ItemPath}' specified an invalid value for the parameter" +
							$" '{field.ParamName}'.");
					}
				}
			}
		}
	}
}
