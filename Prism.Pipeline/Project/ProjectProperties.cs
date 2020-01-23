/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2018-2020 The Spectrum Team
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */
using System;
using System.Collections.Generic;
using System.Linq;

namespace Prism.Pipeline
{
	// Contains project properties (non-path)
	internal class ProjectProperties
	{
		#region Fields
		public readonly bool Compress;
		public readonly uint PackSize;
		public readonly bool IncludeComments;
		public readonly string[] Comments;
		public readonly (string key, string value)[] Params;
		#endregion // Fields

		private ProjectProperties(bool c, uint ps, bool ic, string[] cmts, (string, string)[] pars)
		{
			Compress = c;
			PackSize = ps;
			IncludeComments = ic;
			Comments = cmts;
			Params = pars;
		}

		public static ProjectProperties FromParseResults(ParamSet pars, out string err)
		{
			bool c = false, ic = false;
			uint ps = 0;

			if (!pars.TryGet("!c", out var cstr) || !Boolean.TryParse(cstr, out c))
			{
				err = "missing or invalid compress (!c) field";
				return null;
			}
			if (!pars.TryGet("!sz", out var szstr) || !UInt32.TryParse(szstr, out ps))
			{
				err = "missing or invalid pack size (!sz) field";
				return null;
			}
			if (!pars.TryGet("!ic", out var icstr) || !Boolean.TryParse(icstr, out ic))
			{
				err = "missing or invalid include comments (!ic) field";
				return null;
			}

			pars.CopyCommentsTo(out var comments);
			pars.CopyStandardParamsTo(out var @params);
			err = null;
			return new ProjectProperties(c, Math.Clamp(ps, 1, 2048), ic, comments, @params);
		}
	}
}
