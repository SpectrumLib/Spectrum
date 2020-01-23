/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2018-2020 The Spectrum Team
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */
using Spectrum.Graphics;
using System;
using System.IO;

namespace Spectrum
{
	/// <summary>
	/// Contains application parameters that must be specified at startup.
	/// </summary>
	public sealed class CoreParams
	{
		#region Fields
		/// <summary>
		/// The name of the application.
		/// </summary>
		public readonly string Name;
		/// <summary>
		/// The version of the application.
		/// </summary>
		public readonly Version Version;

		#region Logging
		/// <summary>
		/// The <see cref="LogPolicy"/> instance to use instead of the default library policy. If <c>null</c>, the
		/// default policy will an instance of <see cref="FileLogPolicy"/>, which can also be separately customized.
		/// </summary>
		public LogPolicy DefaultLogPolicy = null;
		/// <summary>
		/// The <see cref="IMessageFormatter"/> instance to use instead of the default library formatter. If
		/// <c>null</c>, the default formatter will be an instance of <see cref="Spectrum.DefaultMessageFormatter"/>, 
		/// which can also be separatedly customized.
		/// </summary>
		public IMessageFormatter DefaultMessageFormatter = null;
		/// <summary>
		/// Sets the path to place the log files in when using the default log policy. Can be specified relative to
		/// the application execution directory. Defaults to the application execution directory. Ignored if a custom 
		/// policy is specified in <see cref="DefaultLogPolicy"/>.
		/// </summary>
		public string LogDirectory = null;
		/// <summary>
		/// Sets the base name for log files with the default log policy. Defaults to the application name. Ignored if
		/// a custom policy is specified in <see cref="DefaultLogPolicy"/>.
		/// </summary>
		public string LogFileName = null;
		/// <summary>
		/// Sets the tag used by the default logger. Defaults to the application name. Ignored if a custom message
		/// formatter is specified in <see cref="DefaultMessageFormatter"/>.
		/// </summary>
		public string DefaultLoggerTag = null;
		/// <summary>
		/// Sets if the default log policy should use asynchronous file logging. Defaults to <c>true</c>. Ignored if a
		/// custom policy is specified in <see cref="DefaultLogPolicy"/>.
		/// </summary>
		public bool UseAsyncLogging = true;
		/// <summary>
		/// Sets if the default log policy should put a timestamp in the name of the log files. Defaults to 
		/// <c>false</c>. Ignored if a custom policy is specified in <see cref="DefaultLogPolicy"/>.
		/// </summary>
		public bool UseLogFileTimestamps = false;
		/// <summary>
		/// Sets if the default log policy should archive old log files. Defaults to <c>true</c>. Ignored if a custom
		/// policy is specified in <see cref="DefaultLogPolicy"/>.
		/// </summary>
		public bool UseLogFileArchiving = true;
		/// <summary>
		/// Sets if the library should attach an internal logger to the application. This optional component will
		/// slightly slow down execution, but will log internal states and events, which may be very helpful for
		/// debugging.
		/// </summary>
		public bool UseInternalLogging = false;
		/// <summary>
		/// The <see cref="IMessageFormatter"/> instance used to format messages generated internally by the library.
		/// This defaults to the same formatter that the normal logging system uses, with a message tag of "Spectrum".
		/// Ignored if <see cref="UseInternalLogging"/> is <c>false</c>.
		/// </summary>
		public IMessageFormatter InternalMessageFormatter = null;
		#endregion // Logging

		#region Graphics
		/// <summary>
		/// Sets if the validation layers are loaded into Vulkan. This should be disabled for Release builds. Defaults
		/// to false.
		/// </summary>
		public bool EnableValidationLayers = false;
		/// <summary>
		/// Controls which special features are enabled on the graphics device. By default, no special features are enabled.
		/// </summary>
		public GraphicsDevice.DeviceFeatures EnabledGraphicsFeatures = default;
		#endregion // Graphics

		#region Content
		/// <summary>
		/// The path to the content pack to use for <see cref="Core.GContent"/>. Defaults to "data/Content.cpak" if null.
		/// </summary>
		public string GlobalContentPath = null;
		/// <summary>
		/// If the <see cref="Core.GContent"/> manager should be created.
		/// </summary>
		public bool LoadGlobalContent = true;
		#endregion // Content
		#endregion // Fields

		/// <summary>
		/// Create a new default set of parameters with the given application name and version.
		/// </summary>
		/// <param name="name">The name of the application.</param>
		/// <param name="version">The version of the application.</param>
		public CoreParams(string name, Version version)
		{
			Name = !String.IsNullOrWhiteSpace(name) ? name : 
				throw new ArgumentException("Cannot have a null or empty string as the application name.", nameof(name));
			Version = version ?? throw new ArgumentNullException(nameof(version));
		}

		/// <summary>
		/// Validates that all of the specified parameters in this instance contain valid values. This method is called
		/// by <see cref="Core"/> when an instance is used to initialize the applciation.
		/// </summary>
		/// <exception cref="InvalidCoreParameterException">One of the parameters has an invalid value.</exception>
		public void Validate()
		{
			// Logging validation
			if (LogDirectory != null)
			{
				if (String.IsNullOrWhiteSpace(LogDirectory))
					throw new InvalidCoreParameterException(nameof(LogDirectory), LogDirectory, "empty path");
				if (!PathUtils.IsValidPath(LogDirectory))
					throw new InvalidCoreParameterException(nameof(LogDirectory), LogDirectory, "invalid file path");
			}
			if (LogFileName != null && !PathUtils.IsValidPath(LogFileName))
				throw new InvalidCoreParameterException(nameof(LogFileName), LogFileName, "invalid characters in file name");
			if (DefaultLoggerTag != null && String.IsNullOrWhiteSpace(DefaultLoggerTag))
				throw new InvalidCoreParameterException(nameof(DefaultLoggerTag), DefaultLoggerTag, "cannot specify empty tag");

			// Logging defaults
			LogDirectory ??= Directory.GetCurrentDirectory();
			LogFileName ??= PathUtils.SanitizeFileName(Name);
			DefaultLoggerTag ??= Name;

			// Content defaults
			GlobalContentPath ??= "data/Content.cpak";
			if (!PathUtils.IsValidPath(GlobalContentPath))
				throw new InvalidCoreParameterException(nameof(GlobalContentPath), GlobalContentPath, "invalid global content path");
		}
	}

	/// <summary>
	/// Exception used to report an invalid parameter in the <see cref="CoreParams"/> instance used to inialize the
	/// application.
	/// </summary>
	public sealed class InvalidCoreParameterException :
		Exception
	{
		#region Fields
		/// <summary>
		/// The name of the parameter that contained the invalid value.
		/// </summary>
		public readonly string Parameter;
		/// <summary>
		/// The invalid value of the parameter.
		/// </summary>
		public readonly object Value;
		#endregion // Fields

		internal InvalidCoreParameterException(string par, object val, string msg) :
			base($"Invalid parameter '{par}' - {msg}")
		{
			Parameter = par;
			Value = val;
		}
	}
}
