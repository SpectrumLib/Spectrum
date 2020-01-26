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
	// Contains a full listing of data about a content project and the items it contains
	internal class ContentProject
	{
		#region Fields
		public readonly ProjectPaths Paths;
		public readonly ProjectProperties Properties;
		public IReadOnlyDictionary<string, string> Params => Properties.Params;

		public IReadOnlyCollection<ContentItem> Items => _items;
		private readonly List<ContentItem> _items;
		#endregion // Fields

		public ContentProject(ProjectPaths paths, ProjectProperties props, List<ContentItem> items)
		{
			Paths = paths;
			Properties = props;
			_items = items;
		}

		// Ensures that the cache and output paths exist
		public bool EnsurePaths()
		{
			try
			{
				Paths.Cache.Refresh();
				Paths.Output.Refresh();
				if (!Paths.Cache.Exists)
					Paths.Cache.Create();
				if (!Paths.Output.Exists)
					Paths.Output.Create();
				return true;
			}
			catch
			{
				return false;
			}
		}

		#region File Load/Save
		public static ContentProject LoadFromFile(string path, out string err)
		{
			// Ensure file
			if (!PathUtils.TryGetFileInfo(path, out var finfo) || !finfo.Exists)
			{
				err = $"The file path '{finfo?.FullName ?? path}' is invalid or does not exist.";
				return null;
			}

			// Load the yaml into memory
			YamlDocument ydoc;
			try
			{
				var ys = new YamlStream();
				ys.Load(new StreamReader(finfo.FullName));
				ydoc = ys.Documents[0];
			}
			catch (Exception e)
			{
				err = $"Unable to parse project file - {e.Message}";
				return null;
			}

			// Parse the project properties and paths
			if (!(ydoc.RootNode["project"] is YamlMappingNode pnode))
			{
				err = "No valid 'project' node in project file.";
				return null;
			}
			ProjectPaths paths;
			ProjectProperties props;
			try
			{
				paths = ProjectPaths.LoadFromYaml(finfo, pnode);
				props = ProjectProperties.LoadFromYaml(pnode);
			}
			catch (Exception e)
			{
				err = e.Message;
				return null;
			}

			// Iterate over the items
			if (!(ydoc.RootNode["items"] is YamlSequenceNode inode))
			{
				err = "No valid 'items' node in project file.";
				return null;
			}
			var ilist = new List<ContentItem>(inode.Children.Count);
			try
			{
				uint iidx = 0;
				foreach (var item in inode.Children)
				{
					ilist.Add(ContentItem.LoadFromYaml(
						paths, (item as YamlMappingNode) ?? throw new ProjectFileException($"Invalid item syntax (node {iidx})")
					));
					++iidx;
				}
			}
			catch (Exception e)
			{
				err = e.Message;
				return null;
			}

			err = null;
			return new ContentProject(paths, props, ilist);
		}

		// Emitting YAML is done manually, for full control over generated syntax
		public static bool SaveToFile(ContentProject proj, string path, out string err)
		{
			// Ensure file
			if (!PathUtils.TryGetFileInfo(path, out var finfo))
			{
				err = $"The file path '{path}' is invalid.";
				return false;
			}
			try
			{
				if (finfo.Exists)
					finfo.Delete();
			}
			catch
			{
				err = $"Unable to overrite existing file '{finfo.FullName}'";
				return false;
			}
			using var stm = finfo.Open(FileMode.Create, FileAccess.Write, FileShare.None);
			using var writer = new StreamWriter(stm);

			// Write the project block and item block header
			StringBuilder sb = new StringBuilder(1024);
			{
				sb.Append("project:"); sb.AppendLine();
				sb.Append("  rdir: "); sb.Append(proj.Paths.Original.r); sb.AppendLine();
				sb.Append("  cdir: "); sb.Append(proj.Paths.Original.c); sb.AppendLine();
				sb.Append("  odir: "); sb.Append(proj.Paths.Original.o); sb.AppendLine();
				sb.Append("  compress: "); sb.Append(proj.Properties.Compress); sb.AppendLine();
				sb.Append("  size: "); sb.Append(proj.Properties.PackSize); sb.AppendLine();
				foreach (var par in proj.Properties.Params)
				{
					sb.Append($"  {par.Key}: {par.Value}"); sb.AppendLine();
				}
				sb.AppendLine();
				sb.Append("items:"); sb.AppendLine();
			}
			writer.Write(sb.ToString());

			// Write each item
			foreach (var item in proj.Items)
			{
				sb.Clear();

				sb.Append("- item: "); sb.Append(item.ItemPath); sb.AppendLine();
				if (item.IsLink)
				{
					sb.Append("  link: "); sb.Append(item.LinkPath); sb.AppendLine();
				}
				sb.Append("  type: "); sb.Append(item.Type); sb.AppendLine();
				foreach (var par in item.Params)
				{
					sb.Append($"  {par.Key}: {par.Value}"); sb.AppendLine();
				}

				writer.Write(sb.ToString());
			}

			writer.WriteLine();
			err = null;
			return true;
		}
		#endregion // File Load/Save
	}

	internal class ProjectFileException : Exception
	{
		public ProjectFileException(string message) :
			base(message)
		{ }

		public ProjectFileException(string message, Exception inner) :
			base(message, inner)
		{ }
	}
}
