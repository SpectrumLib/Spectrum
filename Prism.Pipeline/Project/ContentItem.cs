/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2018-2020 The Spectrum Team
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */
using System;
using System.Collections.Generic;
using System.IO;

namespace Prism.Pipeline
{
	// Contains information about a content item from a project file
	internal class ContentItem
	{
		#region Fields
		public readonly string ItemPath;        // The path of the item, as it appears in the project file
		public readonly string LinkPath;		// The path of the link, as it appears in the project file
		public readonly FileInfo InputFile;		// The path to the true input file (respecting link)
		public readonly FileInfo OutputFile;	// The path to the output file (cache directory)
		public readonly FileInfo CacheFile;		// The path to the cache file (.bcache in cache directory)
		public readonly bool IsLink;			// If the item is a link

		public readonly string ProcessorName;	// Name of the processor
		public readonly bool? IncludeComment;   // The include comment override flag (null = not specified)

		public IReadOnlyCollection<string> Comments => _comments;
		private readonly string[] _comments;
		public IReadOnlyCollection<(string key, string value)> Params => _params;
		private readonly (string key, string value)[] _params;
		#endregion // Fields

		private ContentItem(string ip, string lp, FileInfo @if, FileInfo op, FileInfo cp, bool il, 
			string pn, bool? ic, ParamSet pars)
		{
			ItemPath = ip;
			LinkPath = lp;
			InputFile = @if;
			OutputFile = op;
			CacheFile = cp;
			IsLink = il;
			ProcessorName = pn;
			IncludeComment = ic;
			pars.CopyCommentsTo(out _comments);
			pars.CopyStandardParamsTo(out _params);
		}

		public static ContentItem FromParseResults(ProjectPaths pps, string path, ParamSet pars, out string err)
		{
			if (!PathUtils.TryMakeAbsolutePath(path, pps.Root.FullName, out _))
			{
				err = "the item file path is not valid";
				return null;
			}

			bool il = false;
			string
				@if = (il = pars.TryGet("!l", out var link)) ? link : path,
				op = path + ".bin",
				cp = path + ".bcache",
				pn = pars.TryGet("!p", out var pname) ? pname : "None";
			if (!PathUtils.TryMakeAbsolutePath(@if, pps.Root.FullName, out @if))
			{
				err = "the item link path is not valid";
				return null;
			}
			if (!PathUtils.TryMakeAbsolutePath(op, pps.Cache.FullName, out op))
			{
				err = "the item output path is not valid";
				return null;
			}
			if (!PathUtils.TryMakeAbsolutePath(cp, pps.Cache.FullName, out cp))
			{
				err = "the item cache path is not valid";
				return null;
			}

			bool? ic = null;
			if (pars.TryGet("!ic", out var icmt))
			{
				if (!Boolean.TryParse(icmt, out var icb))
				{
					err = "invalid value for include comment (!ic) key";
					return null;
				}
				ic = icb;
			}

			try
			{
				err = null;
				return new ContentItem(
					path, link, new FileInfo(@if), new FileInfo(op), new FileInfo(cp), il, pn, ic, pars);
			}
			catch (Exception e)
			{
				err = e.Message;
				return null;
			}
		}
	}
}
