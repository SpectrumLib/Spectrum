/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2018-2020 The Spectrum Team
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using YamlDotNet.RepresentationModel;

namespace Prism.Pipeline
{
	// Contains information about a content item from a project file
	internal class ContentItem
	{
		private static readonly List<string> PARAM_IGNORE = new List<string>() { 
			"item", "type", "link"
		};

		#region Fields
		public readonly string ItemPath;        // The path of the item, as it appears in the project file
		public readonly string LinkPath;        // The path of the link, as it appears in the project file
		public readonly string ItemName;		// The final name of the item, as it appears in the content pack
		public readonly FileInfo InputFile;		// The path to the true input file (respecting link)
		public readonly FileInfo OutputFile;	// The path to the output file (cache directory)
		public readonly FileInfo CacheFile;     // The path to the cache file (.bcache in cache directory)
		public readonly string Type;			// The content type, which controls which processor is used
		public readonly List<(string key, string value)> Params;

		public bool IsLink => LinkPath != null;
		#endregion // Fields

		public ContentItem(string ipath, string lpath, string realPath, ProjectPaths paths, string type, List<(string, string)> pars)
		{
			ItemPath = ipath;
			LinkPath = lpath;
			ItemName = GetItemName(ipath);

			InputFile = new FileInfo(realPath);
			OutputFile = new FileInfo(Path.Combine(paths.Cache.FullName, $"{ItemName}.bin"));
			CacheFile = new FileInfo(Path.Combine(paths.Cache.FullName, $"{ItemName}.cache"));

			Type = type;
			Params = pars;
		}

		public static string GetItemName(ReadOnlySpan<char> itemPath)
		{
			StringBuilder sb = new StringBuilder(itemPath.Length);
			var last = ReadOnlySpan<char>.Empty;
			foreach (var comp in itemPath.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar))
			{
				if (comp.Length == 0)
					continue;

				sb.Append(comp);
				sb.Append('.');
				last = comp;
			}

			string name = sb.ToString();
			var ext = Path.GetExtension(last);
			return name.Substring(0, name.Length - ext.Length);
		}

		public static ContentItem LoadFromYaml(ProjectPaths paths, YamlMappingNode node)
		{
			// Get the nodes
			if (!(node["item"] is YamlScalarNode inode))
				throw new ProjectFileException("Invalid or missing 'item' item option");
			if (!(node["type"] is YamlScalarNode tnode))
				throw new ProjectFileException("Invalid or missing 'type' item option");
			if (!((node["link"] ?? new YamlScalarNode("")) is YamlScalarNode lnode))
				throw new ProjectFileException("Invalid 'link' item option");
			lnode = (lnode.Value.Length == 0) ? null : lnode;

			// Extract the paths
			if (!PathUtils.TryMakeAbsolutePath(inode.Value, paths.Root.FullName, out var itemPath))
				throw new ProjectFileException($"Invalid path for item '{inode.Value}'");
			string linkPath = null;
			if ((lnode != null) && !PathUtils.TryMakeAbsolutePath(lnode.Value, paths.Root.FullName, out linkPath))
				throw new ProjectFileException($"Invalid link path for item '{lnode.Value}'");

			// Extract the parameters
			var pars = new List<(string, string)>();
			foreach (var par in node.Children)
			{
				if (!(par.Key is YamlScalarNode key) || PARAM_IGNORE.Contains(key.Value))
					continue;
				if (!(par.Value is YamlScalarNode value))
					continue;

				pars.Add((key.Value, value.Value));
			}

			return new ContentItem(inode.Value, lnode.Value, linkPath ?? itemPath, paths, tnode.Value, pars);
		}
	}
}
