/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2018-2020 The Spectrum Team
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */
using System;

namespace Spectrum
{
	/// <summary>
	/// Describes the importance levels of logged messages. Can be combined to create level masks.
	/// </summary>
	[Flags]
	public enum MessageLevel : byte
	{
		/// <summary>
		/// Value that represents non-error messages about standard program execution.
		/// </summary>
		Info = 0x01,
		/// <summary>
		/// Value that represents recoverable, non-standard program execution or invalid API use that allows program
		/// flow to continue, perhaps with a modified state.
		/// </summary>
		Warn = 0x02,
		/// <summary>
		/// Value that represents non-recoverable, non-standard program execution or invalid API use that may severely
		/// affect, or even terminate, program flow.
		/// </summary>
		Error = 0x04,
		/// <summary>
		/// Value that represents a formatted exception message. Not necessarily tied to any specific message level.
		/// </summary>
		Exception = 0x08,

		/// <summary>
		/// Mask that represents no message types, or a silent logger.
		/// </summary>
		None = 0x00,
		/// <summary>
		/// Mask that represents all message levels.
		/// </summary>
		All = (Info | Warn | Error | Exception),
		/// <summary>
		/// Mask that represents all messages levels for non-standard program execution (Warn and above).
		/// </summary>
		Important = (Warn | Error | Exception)
	}
}
