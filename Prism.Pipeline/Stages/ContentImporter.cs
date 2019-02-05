using System;

namespace Prism
{
	/// <summary>
	/// Base class for implementing content file import logic, the first step in the content pipeline.
	/// </summary>
	/// <typeparam name="Tout">
	/// The type passed to the <see cref="ContentProcessor{Tin, Tout, Twriter}"/> stage containing the imported data.
	/// </typeparam>
	public abstract class ContentImporter<Tout>
		where Tout : class
	{
		#region Fields
		/// <summary>
		/// The type instance describing the data type that this importer produces.
		/// </summary>
		public Type OutputType { get; } = typeof(Tout);
		#endregion // Fields

		/// <summary>
		/// Performs the import step to bring the raw content file into memory for processing.
		/// </summary>
		/// <param name="ctx">The context information about the current import step.</param>
		/// <returns>The data to pass into the content processing step.</returns>
		public abstract Tout Import(ImporterContext ctx);
	}
}
