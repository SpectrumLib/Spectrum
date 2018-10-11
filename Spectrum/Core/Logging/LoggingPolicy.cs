using System;
using System.IO;

namespace Spectrum
{
	/// <summary>
	/// Used to implement custom logging functionality. See <see cref="FileLogPolicy"/> for an example as to how to
	/// implement a custom policy. Policies cannot not be shared between loggers.
	/// </summary>
	public interface ILogPolicy
	{
		/// <summary>
		/// The mask of logging levels that this policy should handle.
		/// </summary>
		LoggingLevel LevelMask { get; }

		/// <summary>
		/// Called when the logger is first initialized to perform startup for the policy.
		/// </summary>
		void Open();
		/// <summary>
		/// Called when the logger is closed.
		/// </summary>
		void Close();

		/// <summary>
		/// Called to handle a new message to the logger.
		/// </summary>
		/// <param name="logger">The logger that generated the message.</param>
		/// <param name="ll">The level of the message, will only be one type, not a mask.</param>
		/// <param name="message">The message text, pre-formatted with extra information.</param>
		void Write(Logger logger, LoggingLevel ll, string message);
	}


	/// <summary>
	/// Default implementation of a log policy that writes to a log file. Used as the default policy by the library
	/// if the user does not specify a custom policy. Has the ability to make asynchronous writes to the file.
	/// </summary>
	public sealed class FileLogPolicy : ILogPolicy
	{
		#region Fields
		// Text writer for the file
		private StreamWriter _writer = null;

		/// <summary>
		/// The absolute path to the log file written to by this policy.
		/// </summary>
		public readonly string FilePath;

		private LoggingLevel _level;
		LoggingLevel ILogPolicy.LevelMask => _level;
		#endregion // Fields

		/// <summary>
		/// Creates a new policy to write messages to the passed file.
		/// </summary>
		/// <param name="fileName">The name of the log file (goes in the application directory).</param>
		/// <param name="level">The mask of message levels written by this policy.</param>
		public FileLogPolicy(string fileName, LoggingLevel level)
		{
			// TODO: put in correct place
			FilePath = Path.Combine(Directory.GetCurrentDirectory(), fileName);
			_level = level;
		}

		void ILogPolicy.Open()
		{
			_writer = new StreamWriter(
				File.Open(FilePath, FileMode.Create, FileAccess.Write, FileShare.None), System.Text.Encoding.UTF8
			);
		}

		void ILogPolicy.Close()
		{
			_writer.Flush();
			_writer.Close();
			_writer.Dispose();
		}

		void ILogPolicy.Write(Logger logger, LoggingLevel ll, string message)
		{
			_writer.WriteLine(message);
		}
	}
}
