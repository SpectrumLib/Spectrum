using System;
using System.Json;

namespace Prism.Content
{
	internal struct ProjectProperties
	{
		#region Fields
		public string RootDir;
		public string IntermediateDir;
		public string OutputDir;
		public bool Pack;
		public CompressionLevel Compression; // 0 = none, 1 = speed, 2 = size
		#endregion // Fields

		// Returns the missing token, or null if everything was present
		public static bool LoadJson(JsonObject obj, out ProjectProperties pp, out string error)
		{
			pp = default;
			error = null;

			// Get the tokens
			if (!obj.TryGetValue("rootDir", out var rDir) || (rDir.JsonType != JsonType.String))
				error = "missing or invalid property 'rootDir'";
			if (!obj.TryGetValue("intermediateDir", out var iDir) || (iDir.JsonType != JsonType.String))
				error = "missing or invalid property 'intermediateDir'";
			if (!obj.TryGetValue("outputDir", out var oDir) || (oDir.JsonType != JsonType.String))
				error = "missing or invalid property 'outputDir'";
			if (!obj.TryGetValue("pack", out var pack) || (pack.JsonType != JsonType.Boolean))
				error = "missing or invalid property 'pack'";
			if (!obj.TryGetValue("compression", out var compress) || (compress.JsonType != JsonType.String))
				error = "missing or invalid property 'compression'";
			var compStr = ((string)compress).ToLower();

			if (error != null)
				return false;

			// Populate the parameters object
			pp.RootDir = (string)rDir;
			pp.IntermediateDir = (string)iDir;
			pp.OutputDir = (string)oDir;
			pp.Pack = (bool)pack;
			pp.Compression =
				(compStr == "none") ? CompressionLevel.None :
				(compStr == "speed") ? CompressionLevel.Speed :
				(compStr == "size") ? CompressionLevel.Size : 
				(CompressionLevel)Byte.MaxValue;

			// Validate the other settings
			if ((byte)pp.Compression == Byte.MaxValue)
			{
				error = $"invalid value for compression ({compStr})";
				return false;
			}

			// No error
			return true;
		}
	}

	// The amount of compression to perform on the content
	internal enum CompressionLevel : byte
	{
		None = 0,
		Speed = 1,
		Size = 2
	}
}
