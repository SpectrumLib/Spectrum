﻿using System;

namespace Prism
{
	// Decodes and dispatches command line 'build', 'rebuild', and 'clean' actions
	public static class CommandLineAction
	{
		// Runs the action passed, with the original command line arguments tacked on (path should be args[1])
		public static int RunAction(string action, string[] args, bool verbose)
		{
			Console.WriteLine($"INFO: Performing command line action '{action}'.");

			// Try to load the content project file
			string filePath = args[1];
			ContentProject project = null;
			try
			{
				project = ContentProject.LoadFromFile(filePath);
				Console.WriteLine($"INFO: Loaded project file at '{project.FilePath}'.");
			}
			catch (Exception e)
			{
				Console.WriteLine($"ERROR: Could not load content project file, reason: {e.Message}.");
				if (verbose && (e.InnerException != null))
				{
					Console.WriteLine($"EXCEPTION: ({e.InnerException.GetType().Name})");
					Console.WriteLine(e.InnerException.StackTrace);
				}
				return -1;
			}

			// Report project information
			if (verbose)
			{
				Console.WriteLine($"INFO: --- Project Info ---\n" +
								  $"      Content Root:       {project.Paths.ContentRoot}\n" +
								  $"      Intermediate Root:  {project.Paths.IntermediateRoot}\n" +
								  $"      Output Root:        {project.Paths.OutputRoot}");
			}

			// Everything went well
			return 0;
		}
	}
}
