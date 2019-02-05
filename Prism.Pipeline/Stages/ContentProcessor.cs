using System;

namespace Prism
{
	/// <summary>
	/// Base class for implementing content file processing logic, the second step in the content pipeline.
	/// </summary>
	/// <typeparam name="Tin">The type containing the data created by a ContentImporter instance.</typeparam>
	/// <typeparam name="Tout">The type containing the data passed to a ContentWriter instance.</typeparam>
	/// <typeparam name="Twriter">The ContentWriter type that writes the data from this processor.</typeparam>
	public abstract class ContentProcessor<Tin, Tout, Twriter>
		where Tin : class
		where Tout : class
		where Twriter : ContentWriter<Tout>
	{
		#region Fields
		/// <summary>
		/// The type instance describing the data type that this processor consumes.
		/// </summary>
		public readonly Type InputType;
		/// <summary>
		/// The type instance describing the data type that this processor produces.
		/// </summary>
		public readonly Type OutputType;
		/// <summary>
		/// The type instance describing the ContentWriter type that writes out the data that this type produces.
		/// </summary>
		public readonly Type WriterType;
		#endregion // Fields

		protected ContentProcessor()
		{
			InputType = typeof(Tin);
			OutputType = typeof(Tout);
			WriterType = typeof(Twriter);
		}
	}
}
