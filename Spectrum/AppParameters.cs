using System;

namespace Spectrum
{
	/// <summary>
	/// Used to pass the parameters for the application to the library on startup. The name and version of the
	/// application are the only required components to provide. The parameters are separated into logical
	/// sections:
	/// <list type="bullet">
	///		<item>General - This controls the name and version of the application.</item>
	///		<item>Logging - Either customize the default logger, or use your own logging classes.</item>
	/// </list>
	/// </summary>
	public struct AppParameters
	{
		#region Fields
		/// <summary>
		/// The official name, or title, of the application.
		/// </summary>
		public readonly string Name;
		/// <summary>
		/// The version of the application.
		/// </summary>
		public readonly AppVersion Version;

		#region Logger
		/// <summary>
		/// Sets the tag for the default logger, see <see cref="Logger.Tag"/>. Defaults to the name of the application.
		/// </summary>
		public string DefaultLoggerTag;
		/// <summary>
		/// Sets if the default logging policy should use asynchronous writes to the log file. Defaults to true.
		/// Ignored if <see cref="DefaultLogPolicy"/> is not null.
		/// </summary>
		public bool UseThreadedLogging;
		/// <summary>
		/// Sets the name of the log file for the default log policy. Defaults to "application". Ignored if
		/// <see cref="DefaultLogPolicy"/> is not null.
		/// </summary>
		public string LogFileBaseName;
		/// <summary>
		/// Sets if a timestamp is used in the default log policy file name. Defaults to true. If true, then the
		/// default log file is named <c>{LogFileBaseName}.{Timestamp}.txt</c>. Note that using this option will 
		/// generate a new log file every time the application is launched, which could potentially clutter the 
		/// filesystem if <see cref="LogFileHistorySize"/> is large. Ignored if <see cref="DefaultLogPolicy"/>
		/// is not null. 
		/// </summary>
		public bool LogFileTimestamp;
		/// <summary>
		/// Sets the number of log files to keep, if <see cref="LogFileTimestamp"/> is true. Log files outside of this
		/// range will be deleted by order of oldest first. Set to 0 to keep no history of log files. Ignored if
		/// <see cref="DefaultLoggingPolicy"/> is not null. Defaults to 5.
		/// </summary>
		public byte LogFileHistorySize;
		/// <summary>
		/// Sets the directory that log files are placed into, relative to the directory the application is in.
		/// Defaults to "logs". Ignored if <see cref="DefaultLoggingPolicy"/> is not null.
		/// </summary>
		public string LogFileDirectory;
		/// <summary>
		/// Sets the formatter used to format logged messages in the library. Defaults to null, which causes the
		/// library to use an instance of <see cref="DefaultLogFormatter"/>.
		/// </summary>
		public ILogFormatter DefaultLoggingFormatter;
		/// <summary>
		/// Sets the default policy used to handle logged messages in the library. Defaults to null, which causes the
		/// library to use an instance of <see cref="FileLogPolicy"/> with the settings given in this class. Do not
		/// manually call <see cref="ILogPolicy.Open"/> on this instance.
		/// </summary>
		public ILogPolicy DefaultLoggingPolicy;
		#endregion // Logger
		#endregion // Fields

		/// <summary>
		/// Creates a new default set of application parameters with the passed name and version.
		/// </summary>
		/// <param name="name">The name of the application.</param>
		/// <param name="version">The version of the application.</param>
		public AppParameters(string name, AppVersion version)
		{
			Name = name;
			Version = version;

			// Logger defaults
			DefaultLoggerTag = name;
			UseThreadedLogging = true;
			LogFileBaseName = "application";
			LogFileTimestamp = true;
			LogFileHistorySize = 5;
			LogFileDirectory = "logs";
			DefaultLoggingFormatter = null;
			DefaultLoggingPolicy = null;
		}

		/// <summary>
		/// Scans through the settings throws an exception for invalid values. Note that this function is called
		/// automatically when the parameters are passed to the constructor for <see cref="SpectrumApp"/>.
		/// </summary>
		public void Validate()
		{
			// Check the general settings
			if (String.IsNullOrWhiteSpace(Name))
				throw new AppParameterException(nameof(Name), "Cannot use a null or whitespace parmeter name");

			// Check the logging settings
			if (!IsValidFS(LogFileBaseName))
			{
				throw new AppParameterException(nameof(LogFileBaseName), $"The base name for the log file " +
					$"'{LogFileBaseName}' contains invalid filesystem characters");
			}
			if (!IsValidFS(LogFileDirectory))
			{
				throw new AppParameterException(nameof(LogFileDirectory), $"The directory for the log files " +
					$"'{LogFileDirectory}' contains invalid filesystem characters");
			}
		}

		// Sanitizes file system paths and names
		private static readonly char[] INVALID_FS_CHARS = System.IO.Path.GetInvalidPathChars();
		private static bool IsValidFS(string name)
		{
			return name.IndexOfAny(INVALID_FS_CHARS) < 0;
		}
	}

	/// <summary>
	/// Represents an invalid parameter in an <see cref="AppParameters"/> instance.
	/// </summary>
	public sealed class AppParameterException : Exception
	{
		#region Fields
		/// <summary>
		/// The name of the invalid parameter.
		/// </summary>
		public readonly string ParameterName;
		#endregion // Fields

		internal AppParameterException(string pname, string message) :
			base(message)
		{
			ParameterName = pname;
		}
	}
}
