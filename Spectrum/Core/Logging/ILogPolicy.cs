/*
 * GNU LGPLv3 License - Copyright (c) The Spectrum Team
 * This file is subject to the terms and conditions of the GNU LGPLv3 license, the text of which can be found in the
 * 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/LGPL-3.0>.
 */
using System;

namespace Spectrum
{
	/// <summary>
	/// Describes types that implement logging output operations.
	/// </summary>
	public interface ILogPolicy
	{
		/// <summary>
		/// The mask of logging levels that this policy should handle.
		/// </summary>
		MessageLevel LevelMask => MessageLevel.All;

		/// <summary>
		/// Called before the policy is first used to perform initialization.
		/// </summary>
		void Initialize();
		/// <summary>
		/// Called when the policy is being destroyed to perform cleanup.
		/// </summary>
		void Terminate();

		/// <summary>
		/// Log the provided message to the policy specific output.
		/// </summary>
		/// <param name="logger">The logger that generated the message.</param>
		/// <param name="ml">The message level for the message.</param>
		/// <param name="msg">The message text to log.</param>
		void Write(Logger logger, MessageLevel ml, string msg);
	}
}
