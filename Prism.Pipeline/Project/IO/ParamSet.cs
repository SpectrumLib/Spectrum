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
	// Contains a listing of parameters from a Prism content project file block
	internal class ParamSet
	{
		#region Fields
		public IReadOnlyCollection<(string k, string v)> Params => _params;
		public IReadOnlyCollection<string> Comments => _comments;

		private readonly List<(string k, string v)> _params;
		private readonly List<string> _comments;
		#endregion // Fields

		public ParamSet()
		{
			_params = new List<(string, string)>();
			_comments = new List<string>();
		}

		public bool TryAdd(string key, string value)
		{
			if (key != "!!")
			{
				var idx = _params.FindIndex(p => p.k == key);
				if (idx != -1)
					return false;
				_params.Add((key, value));
				return true;
			}
			else
			{
				_comments.Add(value);
				return true;
			}
		}

		public bool TryParse(ReadOnlySpan<char> line)
		{
			if (line.IndexOfAny(' ', '\t') is var sidx && (sidx != -1))
			{
				// Trim both values
				var key = line.Slice(0, sidx).ToString();
				var value = (sidx != (line.Length - 1)) ? line.Slice(sidx + 1).TrimStart().ToString() : "";
				return TryAdd(key, value);
			}
			else
				return false;
		}

		public bool TryGet(string key, out string value)
		{
			var idx = _params.FindIndex(p => p.k == key);
			if (idx != -1)
			{
				value = _params[idx].v;
				return true;
			}
			value = null;
			return false;
		}

		public void CopyCommentsTo(out string[] arr) => _comments.CopyTo(arr = new string[_comments.Count]);

		public void CopyStandardParamsTo(out (string, string)[] arr) =>
			arr = _params.Where(p => p.k[0] != '!').ToArray();
	}
}
