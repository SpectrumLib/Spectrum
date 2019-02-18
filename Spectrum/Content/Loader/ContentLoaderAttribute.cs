using System;

namespace Spectrum.Content
{
	/// <summary>
	/// Provides metadata information about <see cref="ContentLoader{T}"/> types. Types that implement content loading
	/// functionality must provide this attribute.
	/// </summary>
	[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
	public sealed class ContentLoaderAttribute : Attribute
	{
		#region Fields
		/// <summary>
		/// The name of the content loader. This name is used to locate the loader using the `LoaderName` value in 
		/// Prism ContentWriter types.
		/// </summary>
		public readonly string Name;
		/// <summary>
		/// Gets if the decorated loader is currently usable. Attempting to use a disabled loader will result in an
		/// exception being thrown.
		/// </summary>
		public readonly bool Enabled;
		#endregion // Fields

		/// <summary>
		/// Creates a new attribute for a <see cref="ContentLoader{T}"/> type.
		/// </summary>
		/// <param name="name">The name of the content loader, must be unique within an assembly.</param>
		/// <param name="enabled">If the decorated type is useable.</param>
		public ContentLoaderAttribute(string name, bool enabled = true)
		{
			Name = name;
			Enabled = enabled;
		}
	}
}
