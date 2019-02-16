using System;
using System.Collections.Generic;
using System.Json;

namespace Prism.Content
{
	internal struct ProjectProperties
	{
		// Constant for converting a value in MB to bytes
		public const uint SIZE_TO_BYTES = 1024 * 1024;

		#region Fields
		public string RootDir;
		public string IntermediateDir;
		public string OutputDir;
		public bool Compress;
		public uint PackSize; // In bytes
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
			if (!obj.TryGetValue("compress", out var compress) || (compress.JsonType != JsonType.Boolean))
				error = "missing or invalid property 'compress'";
			if (!obj.TryGetValue("packSize", out var packSize) || (packSize.JsonType != JsonType.Number))
				error = "missing or invalid property 'packSize'";

			if (error != null)
				return false;

			// Populate the parameters object
			pp.RootDir = (string)rDir;
			pp.IntermediateDir = (string)iDir;
			pp.OutputDir = (string)oDir;
			pp.Compress = (bool)compress;
			{
				long raw = (long)(double)packSize;
				if (raw <= 0 || raw > 2048)
				{
					error = $"the pack size ({raw}) must be between 1MB (1) and 2GB (2048)";
					return false;
				}
				pp.PackSize = (uint)raw * SIZE_TO_BYTES;
			}

			// No error
			return true;
		}

		// Derives a new set of properties from an existing set, potential overrides
		public static void LoadOverrides(in ProjectProperties pp, Dictionary<string, object> os, out ProjectProperties? opp)
		{
			if (os == null)
			{
				opp = null;
				return;
			}

			var copy = pp;
			bool changed = false;

			if (os.ContainsKey("compress"))
			{
				copy.Compress = (bool)os["compress"];
				changed = pp.Compress != copy.Compress;
			}
			if (os.ContainsKey("packSize"))
			{
				copy.PackSize = (uint)os["packSize"] * SIZE_TO_BYTES;
				changed = changed || (pp.PackSize != copy.PackSize);
			}

			opp = changed ? copy : (ProjectProperties?)null;
		}
	}
}
