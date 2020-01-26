﻿/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2018-2020 The Spectrum Team
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */
using System;
using System.Collections.Generic;
using System.Linq;
using YamlDotNet.RepresentationModel;

namespace Prism.Pipeline
{
	// Contains project properties (non-path)
	internal class ProjectProperties
	{
		private static readonly List<string> PARAM_IGNORE = new List<string>() { 
			"rdir", "cdir", "odir", "compress", "size"
		};

		#region Fields
		public readonly bool Compress;
		public readonly uint PackSize;
		public readonly Dictionary<string, string> Params;
		#endregion // Fields

		private ProjectProperties(bool c, uint ps, (string, string)[] pars)
		{
			Compress = c;
			PackSize = ps;
			Params = pars.ToDictionary(par => par.Item1, par => par.Item2);
		}

		public static ProjectProperties LoadFromYaml(YamlMappingNode node)
		{
			// Get the nodes
			if (!node.TryGetChild<YamlScalarNode>("compress", out var cnode))
				throw new ProjectFileException("Invalid or missing 'compress' project option");
			if (!node.TryGetChild<YamlScalarNode>("size", out var snode))
				throw new ProjectFileException("Invalid or missing 'size' project option");

			// Try to convert
			if (!Boolean.TryParse(cnode.Value, out bool compress))
				throw new ProjectFileException("'compress' project option must be a boolean");
			if (!UInt32.TryParse(snode.Value, out uint size))
				throw new ProjectFileException("'size' project option must be unsigned integer");

			// Load the rest of the parameters
			var pars = new List<(string, string)>();
			foreach (var par in node.Children)
			{
				if (!(par.Key is YamlScalarNode key) || PARAM_IGNORE.Contains(key.Value))
					continue;
				if (!(par.Value is YamlScalarNode value))
					continue;

				pars.Add((key.Value, value.Value));
			}

			return new ProjectProperties(compress, size, pars.ToArray());
		}
	}
}
