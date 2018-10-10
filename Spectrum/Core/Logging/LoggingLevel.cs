using System;

namespace Spectrum
{
	/// <summary>
	/// Represents the relative importance and error status of a logging message. Can be combined to create masks.
	/// </summary>
	[Flags]
	public enum LoggingLevel : byte
	{
		/// <summary>
		/// Mask that represents no message types, or a silent logger.
		/// </summary>
		None = 0x00,
		/// <summary>
		/// Type that represents low level debugging information from the library and Vulkan debug callbacks.
		/// </summary>
		Debug = 0x01,
		/// <summary>
		/// Type that represents non-error messages about standard program execution.
		/// </summary>
		Info = 0x02,
		/// <summary>
		/// Type that represents recoverable, non-standard program execution or invalid API use that does not affect
		/// program flow.
		/// </summary>
		Warn = 0x04,
		/// <summary>
		/// Type that represents recoverable, non-standard program execution or invalid API use that affects program
		/// flow and may represent a serious underlying error.
		/// </summary>
		Error = 0x08,
		/// <summary>
		/// Type that represents a non-recoverable error program execution that prevents normal execution of the program
		/// from continuing beyond the error point.
		/// </summary>
		Fatal = 0x10,
		/// <summary>
		/// Type that represents a formatted exception message. Not necessarily tied to any error message types.
		/// </summary>
		Exception = 0x20,
		/// <summary>
		/// Mask that represents all message levels.
		/// </summary>
		All = (Debug | Info | Warn | Error | Fatal | Exception),
		/// <summary>
		/// Mask that represents all message levels without debug messages: non-verbose logging.
		/// </summary>
		Standard = (Info | Warn | Error | Fatal | Exception),
		/// <summary>
		/// Mask that represents all messages levels for non-standard program execution (Warn and above).
		/// </summary>
		Important = (Warn | Error | Fatal | Exception)
	}
}
