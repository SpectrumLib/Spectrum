/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2018-2019 The Spectrum Team
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

		public static ProjectProperties FromParseResults(IReadOnlyCollection<(string key, string value)> values, out string err)
		{
			List<string> comments = new List<string>();
			List<(string, string)> pars = new List<(string, string)>();
			bool c = false, ic = false;
			uint ps = 0;

			if (values.FirstOrDefault(p => p.key == "!c") is var cpair && (cpair.key == null
				|| !Boolean.TryParse(cpair.value, out c)))
			{
				err = "missing or invalid compress (!c) field";
				return null;
			}
			if (values.FirstOrDefault(p => p.key == "!sz") is var szpair && (szpair.key == null
				|| !UInt32.TryParse(szpair.value, out ps)))
			{
				err = "missing or invalid pack size (!sz) field";
				return null;
			}
			if (values.FirstOrDefault(p => p.key == "!ic") is var icpair && (icpair.key == null
				|| !Boolean.TryParse(icpair.value, out ic)))
			{
				err = "missing or invalid include comments (!ic) field";
				return null;
			}

			foreach (var pair in values)
			{
				if (pair.key == "!!")
					comments.Add(pair.value);
				else if (pair.key[0] != '!')
					pars.Add(pair);
			}

			err = null;
			return new ProjectProperties(c, Math.Clamp(ps, 1, 2048), ic, comments.ToArray(), pars.ToArray());
		}
	}
}
