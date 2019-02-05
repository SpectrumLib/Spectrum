using System;

namespace Prism
{
	/// <summary>
	/// Decorates fields in <see cref="ContentProcessor{Tin, Tout, Twriter}"/> types that can have their values set
	/// as parameters given in a content project file.
	/// </summary>
	[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
	public sealed class PipelineParameterAttribute : Attribute
	{
		#region Fields
		/// <summary>
		/// The default value assigned to the parameter.
		/// </summary>
		public readonly object DefaultValue;
		/// <summary>
		/// The optional name of the parameter as it is specified in content projects. If null, the name of the field
		/// that the attribute decorates will be used.
		/// </summary>
		public string Name = null;
		/// <summary>
		/// An optional description of what the parameter affects in the processor.
		/// </summary>
		public string Description = "";
		#endregion // Fields

		/// <summary>
		/// Creates a new pipeline parameter attribute.
		/// </summary>
		/// <param name="defaultValue">
		/// The default value to assign to the parameter if a custom one is not provided. Must be of the same type
		/// as the decorated parameter, or a type convertable to the same type.
		/// </param>
		public PipelineParameterAttribute(object defaultValue)
		{
			DefaultValue = defaultValue;
		}
	}
}
