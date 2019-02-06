using System;

namespace Prism
{
	/// <summary>
	/// Base class for implementing content file writing logic, the last step in the content pipeline.
	/// </summary>
	/// <typeparam name="Tin">The type containing the data created by a <see cref="ContentProcessor{Tin, Tout, Twriter}"/> instance.</typeparam>
	public abstract class ContentWriter<Tin> : IContentWriter
		where Tin : class
	{
		#region Fields
		/// <summary>
		/// The type instance describing the data type that this writer consumes.
		/// </summary>
		public Type InputType { get; } = typeof(Tin);
		#endregion // Fields

		/// <summary>
		/// Writes the processed content data out to the content file to be consumed by a Spectrum application.
		/// </summary>
		/// <param name="input">The processed content data to write.</param>
		/// <param name="writer">The stream used to write the content data to a content file.</param>
		public abstract void Write(Tin input, ContentStream writer);

		// The pipeline will ensure that input is of type Tin before this is called, so null will never be passed accidentally
		void IContentWriter.Write(object input, ContentStream writer) => Write(input as Tin, writer);
	}
}
