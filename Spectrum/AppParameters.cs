using System;

namespace Spectrum
{
	/// <summary>
	/// Used to pass the parameters for the application to the library on startup. The name and version of the
	/// application are the only required components to provide.
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
		}
	}
}
