/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2018-2020 The Spectrum Team
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */
using System;
using System.Diagnostics;
using System.Reflection;

namespace Prism.Pipeline
{
	// Specific exception to report errors from a ContentProcessor instance when processing an item
	internal class PipelineItemException : Exception
	{
		#region Fields
		public readonly ContentItem Item;

		public readonly StackTrace Trace;
		public MethodBase CallingMethod => Trace.GetFrame(0).GetMethod();
		public int CallingLine => Trace.GetFrame(0).GetFileLineNumber();

		public override string StackTrace => Trace.ToString();
		public new MethodBase TargetSite => Trace.GetFrame(0).GetMethod();
		#endregion // Fields

		public PipelineItemException(StackTrace trace, ContentItem item, string msg) :
			base(msg)
		{
			Trace = trace;
			Item = item;
			Source = trace.GetFrame(0).GetMethod().Name;
		}

		public PipelineItemException(StackTrace trace, ContentItem item, string msg, Exception inner) :
			base(msg, inner)
		{
			Trace = trace;
			Item = item;
			Source = trace.GetFrame(0).GetMethod().Name;
		}
	}
}
