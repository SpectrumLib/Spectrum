/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2018-2020 The Spectrum Team
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */
using System;
using Prism.Pipeline;

namespace Prism
{
    // Performs the processing for the command line "new" action
	internal static class NewAction
	{
        public static bool Process()
        {
            // Check the file type
			var ftype = Arguments.ActionArg.ToLowerInvariant() switch { 
				"project" => GeneratedFileType.Project,
				_ => (GeneratedFileType)(-1)
			};
			if ((int)ftype == -1)
			{
				CConsole.Error($"Unknown file type '{Arguments.ActionArg}'.");
				return false;
			}

			// Generate the file
			try
			{
				var path = PrismFileGenerator.GenerateFile(ftype, Arguments.Path);
				if (Arguments.Verbosity >= 0)
					CConsole.Info($"Generated new {Arguments.ActionArg} file at '{path}'.");
				return true;
			}
			catch (Exception e)
			{
				CConsole.Error(e.Message);
				return false;
			}
		}
	}
}
