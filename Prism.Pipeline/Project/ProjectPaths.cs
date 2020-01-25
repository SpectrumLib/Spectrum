/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2018-2020 The Spectrum Team
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */
using System;
using System.IO;
using YamlDotNet.RepresentationModel;

namespace Prism.Pipeline
{
	// Contains path information for a content project
	internal class ProjectPaths
	{
		#region Fields
		// The path to the content project file
		public readonly FileInfo Project;
		// The path to the directory containing the project file
		public DirectoryInfo Directory => Project.Directory;
		// The root (rdir) directory for the input content files
		public readonly DirectoryInfo Root;
		// The cache (cdir) directory for build cache and intermediate files
		public readonly DirectoryInfo Cache;
		// The output (odir) directory for output files
		public readonly DirectoryInfo Output;

		// The paths, as they originally appeared in the file
		public readonly (string r, string c, string o) Original;
		#endregion // Fields

		private ProjectPaths(FileInfo proj, DirectoryInfo root, DirectoryInfo cache, DirectoryInfo output,
			in (string, string, string) orig)
		{
			Project = proj;
			Root = root;
			Cache = cache;
			Output = output;
			Original = orig;
		}

		public static ProjectPaths LoadFromYaml(FileInfo proj, YamlMappingNode node)
		{
			// Get the nodes
			if (!(node["rdir"] is YamlScalarNode rnode))
				throw new ProjectFileException("Invalid or missing 'rdir' project option");
			if (!(node["cdir"] is YamlScalarNode cnode))
				throw new ProjectFileException("Invalid or missing 'cdir' project option");
			if (!(node["odir"] is YamlScalarNode onode))
				throw new ProjectFileException("Invalid or missing 'odir' project option");

			// Parse the paths
			if (!PathUtils.TryMakeAbsolutePath(rnode.Value, proj.Directory.FullName, out var rdir))
				throw new ProjectFileException($"Invalid rdir path '{rnode.Value}'");
			if (!PathUtils.TryMakeAbsolutePath(cnode.Value, proj.Directory.FullName, out var cdir))
				throw new ProjectFileException($"Invalid cdir path '{cnode.Value}'");
			if (!PathUtils.TryMakeAbsolutePath(onode.Value, proj.Directory.FullName, out var odir))
				throw new ProjectFileException($"Invalid odir path '{onode.Value}'");

			return new ProjectPaths(proj, new DirectoryInfo(rdir), new DirectoryInfo(cdir), new DirectoryInfo(odir),
				(rnode.Value, cnode.Value, onode.Value));
		}
	}
}
