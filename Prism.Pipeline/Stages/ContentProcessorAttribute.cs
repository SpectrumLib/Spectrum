using System;

namespace Prism
{
	/// <summary>
	/// Provides metadata information about <see cref="ContentProcessor{Tin, Tout, Twriter}"/> types. Types that
	/// implement content processors must provide this attribute.
	/// </summary>
	[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
	public sealed class ContentProcessorAttribute : Attribute
	{
		#region Fields
		/// <summary>
		/// The display name of the processor. The processor is still uniquely identified by its type name, this name
		/// is just for use in the Prism tool.
		/// </summary>
		public readonly string DisplayName;
		/// <summary>
		/// Sets if the decorated processor is currently usable by the pipeline. Set this to false to prevent the 
		/// pipeline from using the processor when it is auto-detected.
		/// </summary>
		public bool Enabled = true;
		#endregion // Fields

		/// <summary>
		/// Describes the metadata of a content processor type.
		/// </summary>
		/// <param name="name">The display name of the content importer.</param>
		public ContentProcessorAttribute(string name)
		{
			DisplayName = name;
		}
	}
}
