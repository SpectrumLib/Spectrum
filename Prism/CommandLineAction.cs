using System;
using System.Threading;
using System.Threading.Tasks;
using Prism.Build;
using Prism.Content;

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
				// Check for any parameter override cmd line args
				var oargs = ArgParser.Params(args);

				project = ContentProject.LoadFromFile(filePath, oargs);
			}
			catch (Exception e)
			{
				CConsole.Error($"Could not load content project file, reason: {e.Message}.");
				if (verbose && (e.InnerException != null))
				{
					CConsole.Error($"{e.InnerException.GetType().Name}");
					CConsole.Error(e.InnerException.StackTrace);
				}
				return -1;
			}

			// Report project information
			if (verbose)
			{
				CConsole.Info($" ------ Project Info ------ ");
				CConsole.Info($"     Content Root:       {project.Paths.ContentRoot}");
				CConsole.Info($"     Intermediate Root:  {project.Paths.IntermediateRoot}");
				CConsole.Info($"     Output Root:        {project.Paths.OutputRoot}");
			}

			// Get the parallelization info
			uint threadCount = (action == "clean") ? 1 : ArgParser.Parallel(args);
			if (threadCount > 1)
				CConsole.Info($"Using {threadCount} threads for {action} process.");

			// Create the build engine to manage this action
			BuildEngine engine = null;
			try
			{
				engine = new BuildEngine(project, new CommandLineLogger(verbose), threadCount);
			}
			catch (Exception e)
			{
				CConsole.Error($"Unable to create build engine, reason: {e.Message}.");
				if (verbose)
				{
					CConsole.Error($"{e.GetType().Name}");
					CConsole.Error(e.StackTrace);
				}
				return -1;
			}

			// Get the build type
			bool releaseBuild = ArgParser.Release(args);

			// Get the stats flag
			bool useStats = ArgParser.Stats(args);

			// Start the action task and logging
			using (engine)
			{
				try
				{
					// Launch the correct task
					Task task = null;
					switch (action)
					{
						case "build": task = engine.Build(false, releaseBuild, useStats); break;
						case "rebuild": task = engine.Build(true, releaseBuild, useStats); break;
						case "clean": task = engine.Clean(); break;
						default: CConsole.Error($"The action '{action}' was not understood."); return -1; // Should never be reached
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
						CConsole.Error($"Action '{action}' encountered an exception, message: {te?.Message}.");
						if (verbose)
						{
							CConsole.Error($"{te?.GetType().Name}");
							CConsole.Error(te?.StackTrace);
						}
						return -1;
					}
				}
				catch (Exception e)
				{
					CConsole.Error($"Unhandled exception during action '{action}', message: {e.Message}.");
					if (verbose)
					{
						CConsole.Error($"{e.GetType().Name}");
						CConsole.Error(e.StackTrace);
					}
					return -1;
				}
			}

			// Everything went well
			return 0;
		}
	}
}
