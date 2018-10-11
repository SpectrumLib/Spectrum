using System;

namespace Spectrum
{
	/// <summary>
	/// Acts as a named source of logging messages, with an optional override for <see cref="ILogFormatter"/>, instead
	/// of using the default formatter.
	/// </summary>
	public sealed class Logger
	{
		#region Fields
		/// <summary>
		/// The name of the logger, used to uniquely identify the logger. Appears in the default formatter.
		/// </summary>
		public readonly string Name;
		/// <summary>
		/// The name of the logger as it appears in the default formatter. Will always have a length of 8, and will be
		/// <see cref="Name"/> either truncated or left-filled with spaces.
		/// </summary>
		public readonly string Tag;

		/// <summary>
		/// The override formatter to use for the messages. Will be ignored if 
		/// </summary>
		public readonly ILogFormatter Formatter;
		#endregion // Fields

		internal Logger(string name, ILogFormatter formatter = null)
		{
			Name = name;
			Tag = name.Length < 8 ? new string(' ', 8 - name.Length) + name : name.Substring(0, 8);
		}
	}
}
