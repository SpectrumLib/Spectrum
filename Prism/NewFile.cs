using System;
using System.Linq;

namespace Prism
{
	// Decodes and dispatches command line 'new' action
	public static class NewFile
	{
		private static readonly string[] VALID_NEW_TYPES = { "project" };

		// Decodes the arguments, then executes them
		public static int Create(string[] args, bool verbose)
		{
			// Validate args length
			if (args.Length < 3)
			{
				CConsole.Error("Invalid argument count.");
				CConsole.Error("Usage: Prism.exe new <type> <file> [args]");
				return -1;
			}

			// Validate the type
			string newType = args[1];
			if (!VALID_NEW_TYPES.Contains(newType))
			{
				CConsole.Error($"Invalid type '{newType}' specified.");
				CConsole.Error($"Please use one of the following types: {String.Join(", ", VALID_NEW_TYPES)}.");
				return -1;
			}

			// Call out to the pipline, handle any errors
			string newPath = "";
			try
			{
				switch (newType)
				{
					case "project":
						{
							newPath = NewFileGenerator.NewProjectFile(args[2]);
							if (!newPath.EndsWith(".prism"))
								CConsole.Warn("The project file does not end with the standard '.prism' extension.");
							break;
						}
					default: CConsole.Error($"The new file type '{newType}' is not yet implemented."); return -1;
				}
			}
			catch (Exception e)
			{
				CConsole.Error($"Could not create new file, reason: {e.Message}.");
				return -1;
			}

			// Report
			CConsole.Info($"Created a new {newType} file at '{newPath}'.");

			return 0;
		}
	}
}
