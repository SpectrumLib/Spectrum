using System;

namespace Prism
{
	/// <summary>
	/// Base class for implementing content file import logic, the first step in the content pipeline.
	/// </summary>
	/// <typeparam name="Tout">
	/// The type passed to the ContentProcessor stage containing the imported information.
	/// </typeparam>
	public abstract class ContentImporter<Tout>
		where Tout : class
	{
		#region Fields
		/// <summary>
		/// The type instance describing the data type that this importer produces.
		/// </summary>
		public readonly Type OutputType;
		#endregion // Fields

		protected ContentImporter()
		{
			OutputType = typeof(Tout);
		}
	}
}
