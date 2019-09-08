/*
 * GNU LGPLv3 License - Copyright (c) The Spectrum Team
 * This file is subject to the terms and conditions of the GNU LGPLv3 license, the text of which can be found in the
 * 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/LGPL-3.0>.
 */
using System;

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
	}
}
