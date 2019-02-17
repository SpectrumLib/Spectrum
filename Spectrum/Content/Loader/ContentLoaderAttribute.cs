using System;

namespace Spectrum.Content
{
	/// <summary>
	/// Provides metadata information about <see cref="ContentLoader{T}"/> types. Types that implement content loading
	/// functionality must provide this attribute.
	/// </summary>
	public sealed class ContentLoaderAttribute : Attribute
	{
		#region Fields
		/// <summary>
		/// Gets if the decorated loader is currently usable. Attempting to use a disabled loader will result in an
		/// exception being thrown.
		/// </summary>
		public readonly bool Enabled;
		#endregion // Fields

		/// <summary>
		/// Creates a new attribute for a <see cref="ContentLoader{T}"/> type.
		/// </summary>
		/// <param name="enabled">If the decorated type is useable.</param>
		public ContentLoaderAttribute(bool enabled = true)
		{
			Enabled = enabled;
		}
	}
}
