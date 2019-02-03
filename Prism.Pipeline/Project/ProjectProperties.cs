using System;
using System.Json;

namespace Prism
{
	internal struct ProjectProperties
	{
		#region Fields
		public string RootDir;
		public string IntermediateDir;
		public string OutputDir;
		#endregion // Fields

		// Returns the missing token, or null if everything was present
		public static bool LoadJson(JsonObject obj, out ProjectProperties pp, out string missing)
		{
			pp = default;
			missing = null;

			// Get the tokens
			if (!obj.TryGetValue("rootDir", out var rDir))
			{
				missing = "rootDir";
				return false;
			}
			if (!obj.TryGetValue("intermediateDir", out var iDir))
			{
				missing = "intermediateDir";
				return false;
			}
			if (!obj.TryGetValue("outputDir", out var oDir))
			{
				missing = "outputDir";
				return false;
			}

			// Populate the parameters object
			pp.RootDir = rDir;
			pp.IntermediateDir = iDir;
			pp.OutputDir = oDir;

			// No error
			return true;
		}
	}
}
