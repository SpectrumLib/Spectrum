/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2018-2020 The Spectrum Team
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Prism.Pipeline
{
	/// <summary>
	/// Provides information and utility to content processing functions in <see cref="ContentProcessor"/> instances.
	/// </summary>
	public class PipelineContext
	{
		#region Fields
		private protected readonly BuildTask _task;   // Task associated with this
		private protected BuildLogger _logger => _task.Engine.Logger;
		private protected readonly BuildOrder _order; // Order associated with this

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

		private protected PipelineContext(BuildTask task, BuildOrder order)
		{
			_task = task;
			_order = order;

			LoopIndex = 0;
		}

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
