using System;
using System.Security.Cryptography;
using System.Text;

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
		public string LoaderName => WriterInstance.LoaderName;
		public CompressionPolicy Policy => WriterInstance.Policy;
		public readonly uint LoaderHash;

		public ProcessorInstance(ProcessorType type)
		{
			Type = type;
			Instance = type.CreateInstance();
			WriterInstance = type.CreateWriterInstance();
			LoaderHash = StringHash(LoaderName);
		}

		// Resets all settable fields back to default, and sets the ones that are included in the item args
		//  If the parse from the content project string fails, then the field takes on its default value
		public void UpdateFields(BuildEngine engine, BuildEvent evt)
		{
			foreach (var field in Type.Fields)
			{
				var idx = evt.Item.ProcessorArgs.FindIndex(arg => arg.Key == field.ParamName);
				if (idx == -1)
					field.Info.SetValue(Instance, field.DefaultValue);
				else
				{
					if (ConverterCache.Convert(field.FieldType, evt.Item.ProcessorArgs[idx].Value, out object parsed))
						field.Info.SetValue(Instance, parsed);
					else
					{
						engine.Logger.EngineError($"The content item '{evt.Item.ItemPath}' specified an invalid value for the parameter" +
							$" '{field.ParamName}'.");
						field.Info.SetValue(Instance, field.DefaultValue);
					}
				}
			}
		}

		// Computes a unique hash for the given string (MD5), used to reference ContentLoader types in content files
		// While this does not technically guarentee a unique value for each input, the chances of any overlap are so
		//   hilariously small that we dont need to worry about it
		private static uint StringHash(string value)
		{
			using (var hasher = MD5.Create())
			{
				var input = Encoding.UTF8.GetBytes(value);
				var output = hasher.ComputeHash(input);
				uint hash = (uint)(
					(output[0] ^ output[4] ^ output[8] ^ output[12])          |
					((output[1] ^ output[5] ^ output[9] ^ output[13]) << 8)   |
					((output[2] ^ output[6] ^ output[10] ^ output[14]) << 16) |
					((output[3] ^ output[7] ^ output[11] ^ output[15]) << 24));
				return hash;
			}
		}
	}
}
