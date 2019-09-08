/*
 * GNU LGPLv3 License - Copyright (c) The Spectrum Team
 * This file is subject to the terms and conditions of the GNU LGPLv3 license, the text of which can be found in the
 * 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/LGPL-3.0>.
 */
using System;
using System.Text;

namespace Spectrum
{
	/// <summary>
	/// Describes types that format logged messages before they are sent to <see cref="ILogPolicy"/> instances for
	/// output.
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
		void Format(StringBuilder output, Logger logger, MessageLevel ml, DateTime time, string message);
		/// <summary>
		/// Format the given exception into a StringBuilder object.
		/// </summary>
		/// <param name="output">The StringBuilder object to format into, will already be cleared.</param>
		/// <param name="logger">The logger that generated the exception message.</param>
		/// <param name="time">A timestamp of when the message was generated.</param>
		/// <param name="message">The exception to format.</param>
		void Format(StringBuilder output, Logger logger, DateTime time, Exception e);
	}
}
