using System;
using System.Text;

namespace Spectrum
{
	/// <summary>
	/// Used to implement custom formatting for logger messages. See <see cref="DefaultLogFormatter"/> for an example
	/// as to how to implement a custom message formatter.
	/// </summary>
	public interface ILogFormatter
	{
		/// <summary>
		/// Transforms a string message into a final output message.
		/// </summary>
		/// <param name="outStr">The empty string builder to build the message in.</param>
		/// <param name="logger">The source of the message.</param>
		/// <param name="ll">The level of the message.</param>
		/// <param name="message">The unformatted raw logged message.</param>
		void FormatMessage(StringBuilder outStr, Logger logger, LoggingLevel ll, string message);
		/// <summary>
		/// Transforms an exception into a message to be logged.
		/// </summary>
		/// /// <param name="outStr">The empty string builder to build the message in.</param>
		/// <param name="logger">The source of this message.</param>
		/// <param name="e">The exception to format.</param>
		/// <returns>The formatted message sent to <see cref="ILogPolicy"/> instances.</returns>
		void FormatException(StringBuilder outStr, Logger logger, Exception e);
	}

	/// <summary>
	/// The defailt log formatter, which just applies a time and log level tag to the beginning of the message.
	/// Exceptions are formatted to include their type and message in multiple lines. Used as the default
	/// formatter by the library if the user does not specify a custom formatter.
	/// </summary>
	public sealed class DefaultLogFormatter : ILogFormatter
	{
		// Replaces newlines to align messages with the tags
		private const string INDENT_STRING = "\n                          ";
		private const string TAB_INDENT_STRING = "\n                          \t";
		private const string TAB_TAB_INDENT_STRING = "\n                          \t\t";

		void ILogFormatter.FormatMessage(StringBuilder outStr, Logger logger, LoggingLevel ll, string message)
		{
			outStr.Append('[');
			Logging.PutTimeTag(outStr);
			outStr.Append("][");
			outStr.Append(logger.Tag);
			outStr.Append("][");
			outStr.Append(ll.GetLevelTag());
			outStr.Append("]:  ");

			var split = message.Split('\n');
			for (int i = 0; i < split.Length - 1; ++i)
			{
				outStr.Append(split[i]);
				outStr.Append(INDENT_STRING);
			}
			outStr.Append(split[split.Length - 1]);
		}

		void ILogFormatter.FormatException(StringBuilder outStr, Logger logger, Exception e)
		{
			outStr.Append('[');
			Logging.PutTimeTag(outStr);
			outStr.Append("][");
			outStr.Append(logger.Tag);
			outStr.Append("][X]:  ");
			outStr.Append(e.GetType().FullName);
			outStr.Append(INDENT_STRING);
			outStr.Append("Message: ");
			outStr.Append(String.Join(TAB_INDENT_STRING, e.Message.Split('\n')));

			if (e.InnerException != null)
			{
				outStr.Append(INDENT_STRING);
				outStr.Append("Inner Exception: ");
				outStr.Append(e.InnerException.GetType().FullName);
				outStr.Append(TAB_INDENT_STRING);
				outStr.Append("Message: ");
				outStr.Append(String.Join(TAB_TAB_INDENT_STRING, e.InnerException.Message.Split('\n')));
			}

			if (e.StackTrace != null)
			{
				outStr.Append(INDENT_STRING);
				outStr.Append("Stack Trace: ");
				outStr.Append(String.Join(TAB_INDENT_STRING, e.StackTrace.Split('\n')));
			}
		}
	}
}
