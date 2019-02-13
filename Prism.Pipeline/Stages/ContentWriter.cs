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
		private static readonly char[] NAME_SPLIT = { ',' };

		#region Fields
		/// <summary>
		/// The type instance describing the data type that this writer consumes.
		/// </summary>
		public Type InputType { get; } = typeof(Tin);

		/// <summary>
		/// The name of the loader type to use to load content written by this type. It should be of the format
		/// <c>AssemblyName:FullTypeName</c>, where 'AssemblyName' is the name of the .dll to load the type from
		/// (without the .dll at the end), and the 'FullTypeName' should be the full namespace name and type. See
		/// <see cref="GenerateLoaderName(Type)"/>.
		/// </summary>
		public abstract string LoaderName { get; }
		#endregion // Fields

		/// <summary>
		/// Writes the processed content data out to the content file to be consumed by a Spectrum application.
		/// </summary>
		/// <param name="input">The processed content data to write.</param>
		/// <param name="writer">The stream used to write the content data to a content file.</param>
		/// <param name="ctx">The context information about the current processing step.</param>
		public abstract void Write(Tin input, ContentStream writer, WriterContext ctx);

		// The pipeline will ensure that input is of type Tin before this is called, so null will never be passed accidentally
		void IContentWriter.Write(object input, ContentStream writer, WriterContext ctx) => Write(input as Tin, writer, ctx);

		/// <summary>
		/// Utility function for generating a name for a given type that is compliant with the <see cref="LoaderName"/>
		/// field of ContentWriter types. Note that this function does not perform any type checking, it generates the
		/// name assuming it is a valid loader type.
		/// </summary>
		/// <param name="type">The type to generate the name for.</param>
		/// <returns>A valid <see cref="LoaderName"/> name for the type.</returns>
		public static string GenerateLoaderName(Type type)
		{
			if (type == null)
				throw new ArgumentNullException(nameof(type));

			var asmName = type.Assembly.FullName.Split(NAME_SPLIT)[0].Trim();
			var typeName = type.FullName;
			return $"{asmName}:{typeName}";
		}
	}
}
