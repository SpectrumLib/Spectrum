/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2018-2020 The Spectrum Team
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */
using System;
using YamlDotNet.RepresentationModel;

namespace Prism.Pipeline
{
	// Utility extensions for working with YamlDotNet
	internal static class YamlUtils
	{
		// Safe way to try to get a named child of a node
		public static bool TryGetChild<T>(this YamlMappingNode node, YamlNode key, out T value)
			where T : YamlNode
		{
			if (!node.Children.TryGetValue(key, out var rawValue) || !(rawValue is T castValue))
			{
				value = null;
				return false;
			}

			value = castValue;
			return true;
		}

		// Get a child node, or the passed default
		public static YamlNode GetChildOrDefault(this YamlMappingNode node, YamlNode key, YamlNode @default = null)
		{
			if (!node.Children.TryGetValue(key, out var value))
				value = @default;
			return value;
		}
	}
}
