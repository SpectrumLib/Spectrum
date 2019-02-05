using System;

namespace Prism
{
	/// <summary>
	/// Base class for implementing content file processing logic, the second step in the content pipeline.
	/// </summary>
	/// <typeparam name="Tin">The type containing the data created by a <see cref="ContentImporter{Tout}"/> instance.</typeparam>
	/// <typeparam name="Tout">The type containing the data passed to a <see cref="ContentWriter{Tin}"/> instance.</typeparam>
	/// <typeparam name="Twriter">The <see cref="ContentWriter{Tin}"/> type that writes the data from this processor.</typeparam>
	public abstract class ContentProcessor<Tin, Tout, Twriter> : IContentProcessor
		where Tin : class
		where Tout : class
		where Twriter : ContentWriter<Tout>
	{
		#region Fields
		/// <summary>
		/// The type instance describing the data type that this processor consumes.
		/// </summary>
		public Type InputType { get; } = typeof(Tin);
		/// <summary>
		/// The type instance describing the data type that this processor produces.
		/// </summary>
		public Type OutputType { get; } = typeof(Tout);
		/// <summary>
		/// The type instance describing the ContentWriter type that writes out the data that this type produces.
		/// </summary>
		public Type WriterType { get; } = typeof(Twriter);
		#endregion // Fields

		/// <summary>
		/// Performs the processing step to convert the imported content data into the output data.
		/// </summary>
		/// <param name="input">The imported content data from a <see cref="ContentImporter{Tout}"/> instance.</param>
		/// <param name="ctx">The context information about the current processing step.</param>
		/// <returns>The data to pass into the content processing step.</returns>
		public abstract Tout Process(Tin input, ProcessorContext ctx);

		// The pipeline will ensure that input is of type Tin before this is called, so null will never be passed accidentally
		object IContentProcessor.Process(object input, ProcessorContext ctx) => Process(input as Tin, ctx);
	}
}
