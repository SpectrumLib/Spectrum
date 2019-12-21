/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2018-2019 The Spectrum Team
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace Prism.Pipeline
{
	// Parser for .prism files (Prism content project files)
	internal class PrismFileReader : IDisposable
	{
		#region Fields
		private FileInfo _fileInfo;
		private StreamReader _reader;

		private uint _lineNum = 1;

		public ParamSet ProjectParams { get; private set; }
		#endregion // Fields

		public PrismFileReader(string path)
		{
			if (!PathUtils.TryGetFileInfo(path, out _fileInfo) || !_fileInfo.Exists)
				throw new FileNotFoundException($"The file '{_fileInfo?.FullName ?? path}' is invalid or does not exist");
			_reader = new StreamReader(_fileInfo.OpenRead(), Encoding.UTF8);

			try
			{
				loadProject();
			}
			catch (ParseException) { throw; }
			catch (Exception e)
			{
				throw new ParseException($"Internal error: {e.Message}", _lineNum, e);
			}
		}
		~PrismFileReader()
		{
			dispose(false);
		}

		// Reads the initial project block
		private void loadProject()
		{
			string line;

			// Read past any blank lines
			while ((line = _reader.ReadLine()?.Trim()) != null && (line.Length == 0))
				_lineNum++;

			// Check that the first line is the project block
			if (line.Split(' ', '\t') is var split && !(split.Length == 2 && split[0] == "project" && split[1] == "{"))
				throw new ParseException("Invalid project block header", _lineNum);

			// Read in all lines, which must be empty, key-value pairs, or the block close
			bool closed = false;
			ProjectParams = new ParamSet();
			while ((line = _reader.ReadLine()?.Trim()) != null)
			{
				_lineNum += 1;
				if (line.Length == 0)
					continue;

				if (line == "}")
				{
					closed = true;
					break;
				}

				if (!ProjectParams.TryParse(line))
					throw new ParseException("Invalid key/value line in project", _lineNum);
			}

			if (!closed)
				throw new ParseException("Project block was not closed", _lineNum);
		}

		public bool ReadItem(out string path, out ParamSet @params)
		{
			path = null;
			@params = null;

			bool found = false;
			while (_reader.ReadLine()?.Trim() is var line && line != null)
			{
				_lineNum += 1;
				if (line.Length == 0)
					continue;

				// Block close
				if (line == "}")
				{
					if (!found)
						throw new ParseException("Unexpected item close", _lineNum);
					break;
				}

				if (!found)
				{
					// Item block open
					if (parseItemLine(line, out var ipath, out var iempty))
					{
						path = ipath.ToString();
						@params = new ParamSet();
						found = true;
						if (iempty)
							break;  // Return immediately - single line empty item match
						else
							continue;
					}
					else
						throw new ParseException("Expected item block header", _lineNum);
				}
				else
				{
					// key=value pair for params or importer/processor/comment description
					if (@params.TryParse(line))
						continue;
					else
						throw new ParseException("Expected key/value pair", _lineNum);
				}

				// No valid parse
				throw new ParseException("Line could not be parsed", _lineNum);
			}

			return found;
		}

		private bool parseItemLine(string line, out ReadOnlySpan<char> path, out bool empty)
		{
			path = ReadOnlySpan<char>.Empty;
			empty = false;

			var match = Regex.Match(line, @"^item\s*\((.*)\)\s*\{\s*(\})?$", RegexOptions.Singleline);
			if (match.Success)
			{
				path = line.AsSpan(match.Groups[1].Index, match.Groups[1].Value.Length);
				empty = line[^1] == '}';
				return true;
			}

			return false;
		}
		
		#region IDisposable
		public void Dispose()
		{
			dispose(true);
			GC.SuppressFinalize(this);
		}

		private void dispose(bool disposing)
		{
			if (_fileInfo != null)
			{
				if (disposing)
				{
					_reader?.Close();
					_reader?.Dispose();
				}
				_reader = null;
				_fileInfo = null;
			}
		}
		#endregion // IDisposable
	}

	internal class ParseException : Exception
	{
		public readonly uint Line;

		public ParseException(string msg, uint line) :
			base(msg)
		{
			Line = line;
		}

		public ParseException(string msg, uint line, Exception ie) :
			base(msg, ie)
		{
			Line = line;
		}
	}
}
