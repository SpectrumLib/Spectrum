using System;
using System.Threading;
using System.Threading.Tasks;

namespace Prism
{
	// Decodes and dispatches command line 'build', 'rebuild', and 'clean' actions
	public static class CommandLineAction
	{
		// Runs the action passed, with the original command line arguments tacked on (path should be args[1])
		public static int RunAction(string action, string[] args, bool verbose)
		{
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

			// Get the parallelization info
			uint threadCount = (action == "clean") ? 1 : ArgParser.Parallel(args);
			if (threadCount > 1)
				Console.WriteLine($"INFO: Using {threadCount} threads for {action} process.");

			// Create the build engine to manage this action
			BuildEngine engine = null;
			try
			{
				engine = new BuildEngine(project, new CommandLineLogger(verbose), threadCount);
			}
			catch (Exception e)
			{
				Console.WriteLine($"ERROR: Unable to create build engine, reason: {e.Message}.");
				if (verbose)
				{
					Console.WriteLine($"EXCEPTION: ({e.GetType().Name})");
					Console.WriteLine(e.StackTrace);
				}
			}

			// Start the action task and logging
			using (engine)
			{
				try
				{
					// Launch the correct task
					Task task = null;
					switch (action)
					{
						case "build": task = engine.Build(false); break;
						case "rebuild": task = engine.Build(true); break;
						case "clean": task = engine.Clean(); break;
						default: Console.WriteLine($"ERROR: The action '{action}' was not understood."); return -1; // Should never be reached
					}
					task.Start();

					// Wait for the task to finish, logging while we go
					while (!task.IsCompleted)
					{
						Thread.Sleep(50);
						engine.Logger.Poll();
					}

					// Check that the task did not encounter an exception
					if (task.IsFaulted)
					{
						var te = task.Exception.InnerException;
						Console.WriteLine($"ERROR: Action '{action}' encountered an exception, message: {te?.Message}.");
						if (verbose)
						{
							Console.WriteLine($"EXCEPTION: ({te?.GetType().Name})");
							Console.WriteLine(te?.StackTrace);
						}
						return -1;
					}
				}
				catch (Exception e)
				{
					Console.WriteLine($"ERROR: Unhandled exception during action '{action}', message: {e.Message}.");
					if (verbose)
					{
						Console.WriteLine($"EXCEPTION: ({e.GetType().Name})");
						Console.WriteLine(e.StackTrace);
					}
					return -1;
				}
			}

			// Everything went well
			return 0;
		}
	}
}
