/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2018-2020 The Spectrum Team
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;

namespace Prism.Pipeline
{
	/// <summary>
	/// Provides information and utility to content processing functions in <see cref="ContentProcessor"/> instances.
	/// </summary>
	public sealed class PipelineContext
	{
		#region Fields
		private readonly BuildTask _task;   // Task associated with this
		private BuildLogger _logger => _task.Engine.Logger;
		private readonly BuildOrder _order; // Order associated with this

		/// <summary>
		/// The processed name of the item, taken from the input path, with path delimiters replaced with periods and
		/// the extension removed.
		/// </summary>
		public string ItemName => _order.Item.ItemName;
		/// <summary>
		/// The extension of the content item file.
		/// </summary>
		public string ItemExtension => _order.Item.InputFile.Extension;
		/// <summary>
		/// The content type name for the item.
		/// </summary>
		public string ItemType => _order.Item.Type;
		/// <summary>
		/// The absolute path to the input content item in the filesystem.
		/// </summary>
		public string ItemPath => _order.Item.InputFile.FullName;
		/// <summary>
		/// The absolute path to the output file to contain the processed content data.
		/// </summary>
		public string OutputPath => _order.Item.OutputFile.FullName;

		/// <summary>
		/// The set of raw item parameters from the content project file.
		/// </summary>
		public IReadOnlyDictionary<string, string> Params => _order.Item.Params;

		/// <summary>
		/// Zero-based counter for the current iteration of the <see cref="ContentProcessor"/> process loop. Will be 
		/// <c>0</c> for <see cref="ContentProcessor.Begin"/>, and the total number of loop iterations for
		/// <see cref="ContentProcessor.End"/>.
		/// </summary>
		public uint LoopIndex { get; internal set; }
		/// <summary>
		/// If the current <see cref="ContentProcessor"/> process loop iteration is the first iteration.
		/// </summary>
		public bool IsFirstLoop => (LoopIndex == 0);
		#endregion // Fields

		internal PipelineContext(BuildTask task, BuildOrder order)
		{
			_task = task;
			_order = order;

			LoopIndex = 0;
		}

		#region Utility
		/// <summary>
		/// Reserves a temp file for use by a <see cref="ContentProcessor"/>. Temp files can only be used within
		/// a single pipeline execution, but can be shared across different content items within that execution.
		/// <para>
		/// The returned temp file is unique to the processor instance.
		/// </para>
		/// </summary>
		/// <returns>An object describing the temp file.</returns>
		public FileInfo GetTempFile() => new FileInfo(_task.Engine.ReserveTempFile());
		#endregion // Utility

		#region Parameters
		/// <summary>
		/// Gets if the content item has the given parameter specified.
		/// </summary>
		/// <param name="key">The parameter key to check for.</param>
		/// <returns>If the parameter with the given key exists in the parameter set.</returns>
		public bool HasParam(string key) => Params.ContainsKey(key);

		/// <summary>
		/// Attempts to get the value of the parameter with the given key.
		/// </summary>
		/// <param name="key">The parameter key to get.</param>
		/// <param name="value">The value of the parameter.</param>
		/// <returns>If the parameter was found, and its value retrieved.</returns>
		public bool TryGetParam(string key, out string value) => Params.TryGetValue(key, out value);

		/// <summary>
		/// Gets the parameter with the given key, or a default value if the key can't be found.
		/// </summary>
		/// <param name="key">The parameter key to get.</param>
		/// <param name="default">The default value to use if the key does not exist.</param>
		/// <returns>The parameter value, or the default.</returns>
		public string GetParamOrDefault(string key, string @default) => Params.GetValueOrDefault(key, @default);
		#endregion // Parameters

		#region Logging
		/// <summary>
		/// Logs an info/debug level message about the current item to the build pipeline.
		/// </summary>
		/// <param name="msg">The message to log.</param>
		/// <param name="important">If the message should be considered important.</param>
		public void Info(string msg, bool important = false) =>
			_logger.ItemInfo(_order.Item, _order.Index, msg, important);

		/// <summary>
		/// Logs a warning level message about the current item to the build pipeline.
		/// </summary>
		/// <param name="msg">The message to log.</param>
		public void Warn(string msg) => _logger.ItemWarn(_order.Item, _order.Index, msg);

		/// <summary>
		/// Logs an error level message about the current item to the build pipeline.
		/// </summary>
		/// <param name="msg">The message to log.</param>
		public void Error(string msg) => _logger.ItemError(_order.Item, _order.Index, msg);
		#endregion // Logging

		#region Exceptions
		/// <summary>
		/// Throws an exception in a manner that gives the build pipeline additional information about the exception.
		/// Using this function is preferred over directly throwing exceptions in a <see cref="ContentProcessor"/>.
		/// </summary>
		/// <param name="msg">The exception message.</param>
		[MethodImpl(MethodImplOptions.NoInlining)]
		public void Throw(string msg)
		{
			var trace = new StackTrace(1, true);
			throw new PipelineItemException(trace, _order.Item, msg);
		}

		/// <summary>
		/// Throws an exception in a manner that gives the build pipeline additional information about the exception.
		/// Using this function is preferred over directly throwing exceptions in a <see cref="ContentProcessor"/>.
		/// </summary>
		/// <param name="ex">The exception to throw.</param>
		[MethodImpl(MethodImplOptions.NoInlining)]
		public void Throw(Exception ex)
		{
			var trace = new StackTrace(1, true);
			throw new PipelineItemException(trace, _order.Item, ex.Message, ex);
		}

		/// <summary>
		/// Throws an exception in a manner that gives the build pipeline additional information about the exception.
		/// Using this function is preferred over directly throwing exceptions in a <see cref="ContentProcessor"/>.
		/// </summary>
		/// <param name="msg">The exception message.</param>
		/// <param name="inner">The inner exception that generated the state that is throwing the exception.</param>
		[MethodImpl(MethodImplOptions.NoInlining)]
		public void Throw(string msg, Exception inner)
		{
			var trace = new StackTrace(1, true);
			throw new PipelineItemException(trace, _order.Item, msg, inner);
		}
		#endregion // Exceptions
	}
}
