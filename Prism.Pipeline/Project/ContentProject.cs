/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2018-2020 The Spectrum Team
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */
using System;
using System.Collections.Generic;
using System.IO;
using YamlDotNet.RepresentationModel;

namespace Prism.Pipeline
{
	// Contains a full listing of data about a content project and the items it contains
	internal class ContentProject
	{
		#region Fields
		public readonly ProjectPaths Paths;
		public readonly ProjectProperties Properties;
		public IReadOnlyCollection<(string key, string value)> Params => Properties.Params;

		public IReadOnlyCollection<ContentItem> Items => _items;
		private readonly List<ContentItem> _items;
		#endregion // Fields

		public ContentProject(ProjectPaths paths, ProjectProperties props, List<ContentItem> items)
		{
			Paths = paths;
			Properties = props;
			_items = items;
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
