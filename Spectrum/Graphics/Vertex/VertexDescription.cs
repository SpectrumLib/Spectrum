using Spectrum.Utilities;
using System;
using System.Linq;
using System.Runtime.CompilerServices;
using Vk = VulkanCore;

namespace Spectrum.Graphics
{
	/// <summary>
	/// Describes a vertex layout as it pertains to a single backing buffer (all elements with the same binding number).
	/// </summary>
	/// <remarks>Note that these descriptions are not validated before use.</remarks>
	public struct VertexBinding : IEquatable<VertexBinding>
	{
		#region Fields
		/// <summary>
		/// The data size for a single vertex in the backing buffer.
		/// </summary>
		public readonly uint Stride;
		/// <summary>
		/// The elements that make up this vertex.
		/// </summary>
		public readonly VertexElement[] Elements;
		/// <summary>
		/// If this data is per-instance instead of per-vertex.
		/// </summary>
		public readonly bool PerInstance;
		#endregion // Fields

		#region Constructors
		/// <summary>
		/// Describes a new vertex, calculating the correct stride.
		/// </summary>
		/// <param name="elems">The elements of the vertex to describe.</param>
		public VertexBinding(params VertexElement[] elems)
		{
			Stride = elems.Max(e => e.Offset + e.Format.GetSize());
			Elements = new VertexElement[elems.Length];
			Array.Copy(elems, Elements, elems.Length);
			PerInstance = false;
		}

		/// <summary>
		/// Describes a new vertex, calculating the correct stride.
		/// </summary>
		/// <param name="perInstance">If this data is per-instance instead of per-vertex.</param>
		/// <param name="elems">The elements of the vertex to describe.</param>
		public VertexBinding(bool perInstance, params VertexElement[] elems)
		{
			Stride = elems.Max(e => e.Offset + e.Format.GetSize());
			Elements = new VertexElement[elems.Length];
			Array.Copy(elems, Elements, elems.Length);
			PerInstance = perInstance;
		}

		/// <summary>
		/// Describes a new vertex with an explicit stride (which is not checked).
		/// </summary>
		/// <param name="stride">The explicit stride of the vertex data.</param>
		/// <param name="perInstance">If this data is per-instance instead of per-vertex.</param>
		/// <param name="elems">The elements of the vertex to describe.</param>
		public VertexBinding(uint stride, bool perInstance, params VertexElement[] elems)
		{
			Stride = stride;
			Elements = new VertexElement[elems.Length];
			Array.Copy(elems, Elements, elems.Length);
			PerInstance = perInstance;
		}

		/// <summary>
		/// Describes a new vertex, calculating the stride, element locations, and element offsets assuming tight 
		/// packing in both buffer data and shader binding slots.
		/// </summary>
		/// <param name="fmts">The elements of the vertex, in order.</param>
		public VertexBinding(params VertexElementFormat[] fmts)
		{
			uint loc = 0, off = 0;
			var elements = new VertexElement[fmts.Length];
			fmts.ForEach((fmt, idx) => {
				elements[idx] = new VertexElement(loc, fmt, off);
				loc += fmt.GetBindingSize();
				off += fmt.GetSize();
			});

			Stride = off;
			Elements = elements;
			PerInstance = false;
		}

		/// <summary>
		/// Describes a new vertex, calculating the stride, element locations, and element offsets assuming tight 
		/// packing in both buffer data and shader binding slots.
		/// </summary>
		/// <param name="perInstance">If this data is per-instance instead of per-vertex.</param>
		/// <param name="fmts">The elements of the vertex, in order.</param>
		public VertexBinding(bool perInstance, params VertexElementFormat[] fmts)
		{
			uint loc = 0, off = 0;
			var elements = new VertexElement[fmts.Length];
			fmts.ForEach((fmt, idx) => {
				elements[idx] = new VertexElement(loc, fmt, off);
				loc += fmt.GetBindingSize();
				off += fmt.GetSize();
			});

			Stride = off;
			Elements = elements;
			PerInstance = perInstance;
		}
		#endregion // Constructors

		private VertexBinding(uint s, VertexElement[] e, bool pi)
		{
			Stride = s;
			Elements = e;
			PerInstance = pi;
		}

		/// <summary>
		/// Creates a copy of this vertex binding.
		/// </summary>
		/// <returns>An identical vertex layout.</returns>
		public VertexBinding Copy() => new VertexBinding(Stride, PerInstance, Elements);

		public override int GetHashCode() => (int)(Stride ^ (Elements.Length << 9)); // Really not ideal

		public override bool Equals(object obj) => (obj is VertexBinding) && (((VertexBinding)obj) == this);

		bool IEquatable<VertexBinding>.Equals(VertexBinding other) =>
			other.Stride == Stride && other.PerInstance == PerInstance && other.Elements.SequenceEqual(Elements);

		public static bool operator == (in VertexBinding l, in VertexBinding r) =>
			l.Stride == r.Stride && l.PerInstance == r.PerInstance && l.Elements.SequenceEqual(r.Elements);

		public static bool operator != (in VertexBinding l, in VertexBinding r) =>
			l.Stride != r.Stride || l.PerInstance != r.PerInstance || !l.Elements.SequenceEqual(r.Elements);
	}

	/// <summary>
	/// Describes a single element of a vertex.
	/// </summary>
	public struct VertexElement : IEquatable<VertexElement>
	{
		#region Fields
		/// <summary>
		/// The location of the vertex element (in the layout specifier in GLSL).
		/// </summary>
		public uint Location;
		/// <summary>
		/// The offset (in bytes) of the element into a vertex in the backing buffer.
		/// </summary>
		public uint Offset;
		/// <summary>
		/// The format of the element.
		/// </summary>
		public VertexElementFormat Format;
		#endregion // Fields

		/// <summary>
		/// Describes a new vertex element with the passed values.
		/// </summary>
		/// <param name="location">The location of the element in the vertex shader.</param>
		/// <param name="format">The format of the element.</param>
		/// <param name="offset">The offset (in bytes) of the element into a vertex in the backing buffer.</param>
		public VertexElement(uint location, VertexElementFormat format, uint offset)
		{
			Location = location;
			Offset = offset;
			Format = format;
		}

		public override string ToString() => $"{{{Format} {Location}:{Offset}}}";

		public override int GetHashCode() => (int)(Location ^ (Offset << 9) ^ ((int)Format << 18));

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public override bool Equals(object obj) => (obj is VertexElement) && (((VertexElement)obj) == this);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		bool IEquatable<VertexElement>.Equals(VertexElement other) =>
			other.Location == Location && other.Offset == Offset && other.Format == Format;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool operator == (in VertexElement l, in VertexElement r) =>
			l.Location == r.Location && l.Offset == r.Offset && l.Format == r.Format;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool operator != (in VertexElement l, in VertexElement r) =>
			l.Location != r.Location || l.Offset != r.Offset || l.Format != r.Format;
	}
}
