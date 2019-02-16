using System;
using System.Collections.Generic;
using System.Json;

namespace Prism.Content
{
	internal struct ProjectProperties
	{
		#region Fields
		public string RootDir;
		public string IntermediateDir;
		public string OutputDir;
		public bool Compress;
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

			if (error != null)
				return false;

			// Populate the parameters object
			pp.RootDir = (string)rDir;
			pp.IntermediateDir = (string)iDir;
			pp.OutputDir = (string)oDir;
			pp.Compress = (bool)compress;

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
				changed = changed || (pp.Compress != copy.Compress);
			}

			opp = changed ? copy : (ProjectProperties?)null;
		}
	}
}
