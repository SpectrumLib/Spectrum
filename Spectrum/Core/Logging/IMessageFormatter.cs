/*
 * GNU LGPLv3 License - Copyright (c) The Spectrum Team
 * This file is subject to the terms and conditions of the GNU LGPLv3 license, the text of which can be found in the
 * 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/LGPL-3.0>.
 */
using System;
using System.Runtime.CompilerServices;
using System.Text;

namespace Spectrum
{
	/// <summary>
	/// Describes types that format logged messages before they are sent to <see cref="LogPolicy"/> instances for
	/// output. Types that implement this should be thread-safe unless it is known that only one thread will be
	/// accessing an instance at a time.
	/// </summary>
	public interface IMessageFormatter
	{
		/// <summary>
		/// Format the given message text into a StringBuilder object.
		/// </summary>
		/// <param name="output">The StringBuilder object to format into, will already be cleared.</param>
		/// <param name="logger">The logger that generated the message.</param>
		/// <param name="ml">The level of the message.</param>
		/// <param name="time">A timestamp of when the message was generated.</param>
		/// <param name="message">The message text.</param>
		void Format(StringBuilder output, Logger logger, MessageLevel ml, DateTime time, ReadOnlySpan<char> message);
		/// <summary>
		/// Format the given exception into a StringBuilder object.
		/// </summary>
		/// <param name="output">The StringBuilder object to format into, will already be cleared.</param>
		/// <param name="logger">The logger that generated the exception message.</param>
		/// <param name="time">A timestamp of when the message was generated.</param>
		/// <param name="message">The exception to format.</param>
		void Format(StringBuilder output, Logger logger, DateTime time, Exception e);
	}

	/// <summary>
	/// The default log message formatter for the library. Appends the time, level, and logger name to the beginning
	/// of the message. Also properly handles multi-line messages.
	/// </summary>
	public sealed class DefaultMessageFormatter : IMessageFormatter
	{
		// Tag format: 'HH:MM:SS.CC|$LOGNAME|*|  '  - length: 25

		private static readonly char[] LEVEL_TAGS = { 'I', 'W', ' ', 'E' };
		private static readonly string INDENT = Environment.NewLine + new string(' ', 25);
		private static readonly string TAB_INDENT = INDENT + "    ";

		#region Fields
		/// <summary>
		/// Flags if the formatter should log exception stack traces.
		/// </summary>
		public bool LogStackTrace = true;
		#endregion // Fields

		void IMessageFormatter.Format(StringBuilder output, Logger logger, MessageLevel ml, DateTime time, ReadOnlySpan<char> message)
		{
			// Build the tag
			Span<char> tag = stackalloc char[25];
			tag[11] = tag[20] = tag[22] = '|';
			tag[23] = tag[24] = ' ';
			PutTimestamp(tag, time);
			logger.Tag.CopyTo(tag.Slice(12, 8));
			tag[21] = LEVEL_TAGS[(int)ml - 1];
			output.Append(tag);

			// Write the lines
			foreach (var line in message.Split('\n'))
			{
				output.Append(line);
				output.Append(INDENT);
			}
			output.Length -= INDENT.Length; // Removes the last indent line in a very cheap way
		}

		void IMessageFormatter.Format(StringBuilder output, Logger logger, DateTime time, Exception e)
		{
			// Build the tag
			Span<char> tag = stackalloc char[25];
			tag[11] = tag[20] = tag[22] = '|';
			tag[23] = tag[24] = ' ';
			PutTimestamp(tag, time);
			logger.Tag.CopyTo(tag.Slice(12, 8));
			tag[21] = 'X';
			output.Append(tag);

			// Write the exception
			output.Append(e.GetType().FullName);
			output.Append(" - ");
			output.Append(e.Message);

			// Write the inner exception
			if (e.InnerException != null)
			{
				output.Append(INDENT);
				output.Append("Inner: ");
				output.Append(e.InnerException.GetType().FullName);
				output.Append(" - ");
				output.Append(e.InnerException.Message);
			}

			// Write the stack trace
			if (e.StackTrace != null && LogStackTrace)
			{
				output.Append(INDENT);
				output.Append("Stack Trace:");
				foreach (var line in e.StackTrace.AsSpan().Split('\n'))
				{
					output.Append(TAB_INDENT);
					output.Append(line);
				}
				output.Length -= INDENT.Length; // Removes the last indent line in a very cheap way
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
		private static void PutTimestamp(Span<char> tag, DateTime time)
		{
			tag[0] = (char)('0' + (time.Hour / 10));
			tag[1] = (char)('0' + (time.Hour % 10));
			tag[2] = ':';
			tag[3] = (char)('0' + (time.Minute / 10));
			tag[4] = (char)('0' + (time.Minute % 10));
			tag[5] = ':';
			tag[6] = (char)('0' + (time.Second / 10));
			tag[7] = (char)('0' + (time.Second % 10));
			tag[8] = '.';
			int cs = time.Millisecond / 10;
			tag[9] = (char)('0' + (cs / 10));
			tag[10] = (char)('0' + (cs % 10));
		}
	}
}
