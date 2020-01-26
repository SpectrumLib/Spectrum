/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2018-2020 The Spectrum Team
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
				$"\n  Pack Size:      {proj.Properties.PackSize}"
			);
			Console.Write(
				$"\n  Parameters:     ({proj.Params.Count})"
			);
			if (Arguments.Verbosity > 0 && proj.Params.Count > 0) Console.Write(
				$"\n    {String.Join("\n    ", proj.Params.Select(p => $"{p.Key} = {p.Value}"))}"
			);
			Console.Write("\n\n");

			// Print file information
			Console.Write(
				$"\nFiles|            Count: {proj.Items.Count}" +
				$"\n-----/"
			);
			if (Arguments.Verbosity >= 0)
			{
				foreach (var item in proj.Items)
					PrintItem(item); 
			}
			Console.Write("\n\n");

			return true;
		}

		private static void PrintItem(ContentItem item)
		{
			Console.Write($"\n  {item.ItemPath} {(item.IsLink ? $"[{item.LinkPath}]" : "")}");
			if (Arguments.Verbosity > 0)
				Console.Write($"\n    Type:         {item.Type}");
			if (Arguments.Verbosity > 1)
			{
				Console.Write($"\n    Parameters:   ({item.Params.Count})");
				if (item.Params.Count > 0)
					Console.Write($"\n        {String.Join("\n        ", item.Params.Select(p => $"{p.Key} = {p.Value}"))}");
			}
		}
	}
}
