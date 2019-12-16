/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2018-2019 The Spectrum Team
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */
using System;
using System.Collections.Generic;
using System.IO;

namespace Prism.Pipeline
{
	// Contains a full listing of data about a content project and the items it contains
	internal class ContentProject
	{
		#region Fields
		public readonly ProjectPaths Paths;
		public readonly ProjectProperties Properties;
		public IReadOnlyCollection<(string key, string value)> Params => Properties.Params;
		public IReadOnlyCollection<string> Comments => Properties.Comments;

		public IReadOnlyCollection<ContentItem> Items => _items;
		private readonly List<ContentItem> _items;
		#endregion // Fields

		private ContentProject(ProjectPaths paths, ProjectProperties props, List<ContentItem> items)
		{
			Paths = paths;
			Properties = props;
			_items = items;
		}

		public static ContentProject LoadFromFile(string path, out string err)
		{
			if (!PathUtils.TryMakeAbsolutePath(path, ".", out var filepath))
			{
				err = "invalid path";
				return null;
			}

			// Load the file reader (and project info)
			try
			{
				using var reader = new PrismFileReader(filepath);
				if (ProjectPaths.FromParseResults(filepath, reader.ProjectParams, out err) is var paths && paths == null)
					return null;
				if (ProjectProperties.FromParseResults(reader.ProjectParams, out err) is var props && props == null)
					return null;

				var items = new List<ContentItem>();
				while (reader.ReadItem(out var ipath, out var ipars))
				{
					var item = ContentItem.FromParseResults(paths, ipath, ipars, out var ierr);
					if (item == null)
					{
						err = $"item {ipath} - {ierr}";
						return null;
					}
					items.Add(item);
				}

				return new ContentProject(paths, props, items);
			}
			catch (ParseException e)
			{
				err = $"reader error (line {e.Line}) - {e.Message}";
				return null;
			}
			catch (FileNotFoundException)
			{
				err = "file does not exist";
				return null;
			}
		}
	}
}
