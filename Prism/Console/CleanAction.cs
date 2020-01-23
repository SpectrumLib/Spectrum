/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2018-2020 The Spectrum Team
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */
using System;
using System.Threading;
using Prism.Pipeline;

namespace Prism
{
	// Performs the processing for the 'clean' action
	internal static class CleanAction
	{
		public static bool Process()
		{
			// Load the content project
			if (ContentProject.LoadFromFile(Arguments.Path, out var err) is var proj && proj == null)
			{
				CConsole.Error($"Unable to load project file - {err}.");
				return false;
			}

			// Create the engine
			BuildEngine engine = null;
			try
			{
				engine = new BuildEngine(proj, new ConsoleLogger(), 1);
			}
			catch (Exception e)
			{
				CConsole.Error($"Unable to create build engine - {e.Message}");
				return false;
			}

			// Run the clean task
			using (engine)
			{
				try
				{
					bool shouldCancel = false;
					Console.CancelKeyPress += (o, args) => {
						CConsole.Warn("Keyboard interrupt received, cancelling task...");
						shouldCancel = true;
						args.Cancel = true; // Cancel the exit, we will exit gracefully once cancelled
					};

					var task = engine.Clean();
					task.Start();

					while (!task.IsCompleted)
					{
						if (shouldCancel)
						{
							engine.Cancel().Wait();
							break;
						}
						Thread.Sleep(10);
					}

					if (task.IsFaulted)
					{
						var te = task.Exception.InnerException;
						CConsole.Error($"Unhandled clean exception ({te?.GetType().Name}) - {te?.Message}.");
						if (Arguments.Verbosity > 0)
							CConsole.Error(te?.StackTrace);
						return false;
					}
				}
				catch (Exception e)
				{
					CConsole.Error($"Unable to run clean action ({e.GetType().Name}) - {e.Message}.");
					if (Arguments.Verbosity > 0)
						CConsole.Error(e.StackTrace);
					return false;
				}
			}

			return true;
		}
	}
}
