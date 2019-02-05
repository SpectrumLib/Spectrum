using System;

namespace Prism
{
	/// <summary>
	/// Base class for implementing content file writing logic, the last step in the content pipeline.
	/// </summary>
	/// <typeparam name="Tin">The type containing the data created by a <see cref="ContentProcessor{Tin, Tout, Twriter}"/> instance.</typeparam>
	public abstract class ContentWriter<Tin>
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
		public abstract void Write(Tin input);
	}
}
