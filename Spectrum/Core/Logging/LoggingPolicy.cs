using System;
using System.IO;

namespace Spectrum
{
	/// <summary>
	/// Used to implement custom logging functionality. See <see cref="DefaultLogPolicy"/> for an example as to how to
	/// implement a custom policy.
	/// </summary>
	public interface ILogPolicy
	{
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
		/// <param name="logger">The source logger for the message.</param>
		/// <param name="ll">The level of the message, will only be one type, not a mask.</param>
		/// <param name="message">The message text, pre-formatted with extra information.</param>
		void Write(Logger logger, LoggingLevel ll, string message);
	}


	/// <summary>
	/// Default implementation of a log policy that writes to a log file. Used as the default policy by the library
	/// if the user does not specify a custom policy.
	/// </summary>
	public sealed class DefaultLogPolicy : ILogPolicy
	{
		#region Fields
		// Text writer for the file
		private StreamWriter _writer = null;

		/// <summary>
		/// The absolute path to the log file written to by this policy.
		/// </summary>
		public readonly string FilePath;
		#endregion // Fields

		/// <summary>
		/// Creates a new policy to write messages to the passed file.
		/// </summary>
		/// <param name="fileName">The name of the log file (goes in the application directory).</param>
		public DefaultLogPolicy(string fileName)
		{
			// TODO: put in correct place
			FilePath = Path.Combine(Directory.GetCurrentDirectory(), fileName);
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
