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
	// Performs the processing for the 'build' and 'rebuild' actions
	internal static class BuildAction
	{
		public static bool Process(bool rebuild)
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
				engine = new BuildEngine(proj, new ConsoleLogger(), Arguments.Parallel);
			}
			catch (Exception e)
			{
				CConsole.Error($"Unable to create build engine - {e.Message}");
				return false;
			}

			// Run the build task
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

					var settings = new BuildSettings {
						Rebuild = rebuild,
						Release = !Arguments.Debug,
						HighCompression = Arguments.HighCompression
					};

					var task = engine.Build(settings);
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
						CConsole.Error($"Unhandled build exception ({te?.GetType().Name}) - {te?.Message}.");
						if (Arguments.Verbosity > 0)
							CConsole.Error(te?.StackTrace);
						return false;
					}
				}
				catch (Exception e)
				{
					CConsole.Error($"Unable to run build action ({e.GetType().Name}) - {e.Message}.");
					if (Arguments.Verbosity > 0)
						CConsole.Error(e.StackTrace);
					return false;
				}
			}

			return true;
		}
	}
}
