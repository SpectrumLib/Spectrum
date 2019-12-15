/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2018-2019 The Spectrum Team
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */
using System;
using System.Linq;
using Prism.Pipeline;

namespace Prism
{
	// Performs the processing for the command line "view" action
	internal static class ViewAction
	{
		public static bool Process()
		{
			// Load the content project
			if (ContentProject.LoadFromFile(Arguments.Path, out var err) is var proj && proj == null)
			{
				CConsole.Error($"Unable to load project file - {err}.");
				return false;
			}

			// Print overall project information
			Console.Write(
				 "\nProject|" +
				 "\n-------/" +
				$"\n  File Path:      {proj.Paths.Project.FullName}" +
				$"\n  Root Path:      {proj.Paths.Root.FullName}" +
				$"\n  Cache Path:     {proj.Paths.Cache.FullName}" +
				$"\n  Output Path:    {proj.Paths.Output.FullName}" +
				$"\n  Compress:       {proj.Properties.Compress}" +
				$"\n  Pack Size:      {proj.Properties.PackSize}" +
				$"\n  Inc. Comments:  {proj.Properties.IncludeComments}"
			);
			Console.Write(
				$"\n  Parameters:     ({proj.Params.Count})"
			);
			if (Arguments.Verbosity > 0) Console.Write(
				$"\n    {String.Join("\n    ", proj.Params.Select(p => $"{p.key} = {p.value}"))}"
			);
			Console.Write(
				$"\n  Comments:       ({proj.Comments.Count})"
			);
			if (Arguments.Verbosity > 1) Console.Write(
				$"\n    {String.Join("\n    ", proj.Comments)}"
			);
			Console.Write("\n\n");

			return true;
		}
	}
}
