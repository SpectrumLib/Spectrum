/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2018-2019 The Spectrum Team
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Prism.Pipeline
{
	// Contains path information for a content project
	internal class ProjectPaths
	{
		#region Fields
		// The path to the content project file
		public readonly FileInfo Project;
		// The path to the directory containing the project file
		public DirectoryInfo ProjectDirectory => Project.Directory;
		// The root (!rp) directory for the input content files
		public readonly DirectoryInfo Root;
		// The cache (!cp) directory for build cache and intermediate files
		public readonly DirectoryInfo Cache;
		// The output (!op) directory for output files
		public readonly DirectoryInfo Output;
		#endregion // Fields

		private ProjectPaths(FileInfo p, DirectoryInfo r, DirectoryInfo c, DirectoryInfo o)
		{
			Project = p;
			Root = r;
			Cache = c;
			Output = o;
		}

		public static ProjectPaths FromParseResults(string path, ParamSet pars, out string err)
		{
			var project = new FileInfo(path);
			var dir = project.Directory.FullName;

			if (!pars.TryGet("!rp", out var rp) || !PathUtils.TryMakeAbsolutePath(rp, dir, out var rpfull))
			{
				err = "missing or invalid root path (!rp) entry";
				return null;
			}
			if (!pars.TryGet("!cp", out var cp) || !PathUtils.TryMakeAbsolutePath(cp, dir, out var cpfull))
			{
				err = "missing or invalid cache path (!cp) entry";
				return null;
			}
			if (!pars.TryGet("!op", out var op) || !PathUtils.TryMakeAbsolutePath(op, dir, out var opfull))
			{
				err = "missing or invalid output path (!op) entry";
				return null;
			}

			err = null;
			return new ProjectPaths(project, new DirectoryInfo(rpfull), new DirectoryInfo(cpfull), new DirectoryInfo(opfull));
		}
	}
}
