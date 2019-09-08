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
		/// <param name="ver">The version of the application.</param>
		public CoreParams(string name, Version ver)
		{
			Name = !String.IsNullOrWhiteSpace(name) ? name : 
				throw new ArgumentException("Cannot have a null or empty string as the application name.", nameof(name));
			Version = ver;
		}
	}
}
