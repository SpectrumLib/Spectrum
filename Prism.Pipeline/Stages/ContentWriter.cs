using System;

namespace Prism
{
	/// <summary>
	/// Base class for implementing content file writing logic, the last step in the content pipeline.
	/// </summary>
	/// <typeparam name="Tin">The type containing the data created by a ContentProcessor instance.</typeparam>
	public abstract class ContentWriter<Tin>
		where Tin : class
	{
		#region Fields
		/// <summary>
		/// The type instance describing the data type that this writer consumes.
		/// </summary>
		public readonly Type InputType;
		#endregion // Fields

		protected ContentWriter()
		{
			InputType = typeof(Tin);
		}
	}
}
