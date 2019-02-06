using System;
using System.Collections.Generic;
using System.Linq;

namespace Prism
{
	/// <summary>
	/// Provides metadata information about <see cref="ContentImporter{Tout}"/> types. Types that implement content
	/// importers must provide this attribute.
	/// </summary>
	[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
	public sealed class ContentImporterAttribute : Attribute
	{
		#region Fields
		/// <summary>
		/// The display name of the importer. The importer is still uniquely identified by its type name, this name
		/// is just for use in the Prism tool.
		/// </summary>
		public readonly string DisplayName;
		/// <summary>
		/// The type of the default processor to use with this importer.
		/// </summary>
		public readonly Type DefaultProcessor;

		private readonly string[] _extensions;
		/// <summary>
		/// The file extensions that this importer is the default for, prefixed with a '.'.
		/// </summary>
		public IEnumerable<string> Extensions => _extensions;
		#endregion // Fields

		/// <summary>
		/// Describes the metadata of a content importer type.
		/// </summary>
		/// <param name="name">The display name of the content importer.</param>
		/// <param name="defaultProcessor">The type of the default processor to use with this importer.</param>
		/// <param name="extensions">A list of the extensions that this importer is the default for.</param>
		public ContentImporterAttribute(string name, Type defaultProcessor, params string[] extensions)
		{
			DisplayName = name;
			DefaultProcessor = defaultProcessor;
			_extensions = extensions.Select(ext => ext.StartsWith(".") ? ext : '.' + ext).ToArray();
		}
	}
}
