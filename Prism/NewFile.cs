using System;
using System.Linq;

namespace Prism
{
	// Decodes and dispatches command line 'new' commands
	public static class NewFile
	{
		private static readonly string[] VALID_NEW_TYPES = { "project" };

		// Decodes the arguments, then executes them
		public static int Create(string[] args, bool verbose)
		{
			// Validate args length
			if (args.Length < 3)
			{
				Console.WriteLine("ERROR: Invalid argument count.");
				Console.WriteLine("Usage: Prism.exe new <type> <file> [args]");
				return -1;
			}

			// Validate the type
			string newType = args[1];
			if (!VALID_NEW_TYPES.Contains(newType))
			{
				Console.WriteLine($"ERROR: Invalid type '{newType}' specified.");
				Console.WriteLine($"Please use one of the following types: {String.Join(", ", VALID_NEW_TYPES)}.");
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
								Console.WriteLine("WARNING: The project file does not end with the standard '.prism' extension.");
							break;
						}
					default: Console.WriteLine($"ERROR: The new file type '{newType}' is not yet implemented."); return -1;
				}
			}
			catch (Exception e)
			{
				Console.WriteLine($"ERROR: Could not create new file, reason: {e.Message}.");
				return -1;
			}

			// Report
			Console.WriteLine($"INFO: Created a new {newType} file at '{newPath}'.");

			return 0;
		}
	}
}
