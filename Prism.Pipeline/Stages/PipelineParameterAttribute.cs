using System;

namespace Prism
{
	/// <summary>
	/// Decorates fields in <see cref="ContentProcessor{Tin, Tout, Twriter}"/> types that can have their values set
	/// as parameters given in a content project file. If a <code>readonly</code> field is decorated with this
	/// attribute, it is ignored and cannot be used. This field can only decorate certain value types and strings.
	/// <para>
	/// The full list of valid decorated types is:
	/// <list type="bullet">
	///		<item>All standard signed and unsigned integer types (i.e. <c>sbyte</c> to <c>ulong</c>).</item>
	///		<item>All standard floating point types (i.e. <c>float</c>, <c>double</c>, and <c>decimal</c>).</item>
	///		<item><c>string</c>s.</item>
	/// </list>
	/// </para>
	/// <para>
	/// For every content item, each parameter will be set back to its default value, which is the value it is
	/// assigned in a new unaltered instance of the processor. Other fields, not decorated with this attribute,
	/// will not have their values changed by the build engine.
	/// </para>
	/// </summary>
	[AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = false)]
	public sealed class PipelineParameterAttribute : Attribute
	{
		internal static readonly Type[] VALID_TYPES = {
			typeof(sbyte), typeof(byte), typeof(short), typeof(ushort), typeof(int), typeof(uint), typeof(long),
			typeof(ulong), typeof(float), typeof(double), typeof(decimal), typeof(string)
		};

		#region Fields
		/// <summary>
		/// The optional name of the parameter as it is specified in content projects. If null, the name of the field
		/// that the attribute decorates will be used.
		/// </summary>
		public readonly string Name;
		/// <summary>
		/// An optional description of what the parameter affects in the processor.
		/// </summary>
		public readonly string Description;

		internal object DefaultValue; // The default value for the attribute, assigned by the build engine
		#endregion // Fields

		/// <summary>
		/// Creates a new pipeline parameter attribute.
		/// </summary>
		/// <param name="name">The value for the <see cref="Name"/> field.</param>
		/// <param name="description">The value for the <see cref="Description"/> field.</param>
		public PipelineParameterAttribute(string name = null, string description = "")
		{
			Name = name;
			Description = description;
		}
	}
}
