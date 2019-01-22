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
		/// The index for the backing buffer for this vertex data.
		/// </summary>
		public readonly uint Binding;
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
		/// Describes a new vertex, with a binding index of zero, calculating the correct stride (assuming tight packing).
		/// </summary>
		/// <param name="elems">The elements of the vertex to describe.</param>
		public VertexBinding(params VertexElement[] elems)
		{
			Stride = (uint)elems.Sum(e => e.Format.GetSize());
			Binding = 0;
			Elements = new VertexElement[elems.Length];
			Array.Copy(elems, Elements, elems.Length);
			PerInstance = false;
		}

		/// <summary>
		/// Describes a new vertex, with a specified binding index, calculating the correct stride (assuming tight packing).
		/// </summary>
		/// <param name="binding">The binding index of the backing buffer.</param>
		/// <param name="elems">The elements of the vertex to describe.</param>
		public VertexBinding(uint binding, params VertexElement[] elems)
		{
			Stride = (uint)elems.Sum(e => e.Format.GetSize());
			Binding = binding;
			Elements = new VertexElement[elems.Length];
			Array.Copy(elems, Elements, elems.Length);
			PerInstance = false;
		}

		/// <summary>
		/// Describes a new vertex, with a specified binding index, calculating the correct stride (assuming tight packing).
		/// </summary>
		/// <param name="binding">The binding index of the backing buffer.</param>
		/// <param name="perInstance">If this data is per-instance instead of per-vertex.</param>
		/// <param name="elems">The elements of the vertex to describe.</param>
		public VertexBinding(uint binding, bool perInstance, params VertexElement[] elems)
		{
			Stride = (uint)elems.Sum(e => e.Format.GetSize());
			Binding = binding;
			Elements = new VertexElement[elems.Length];
			Array.Copy(elems, Elements, elems.Length);
			PerInstance = perInstance;
		}

		/// <summary>
		/// Describes a new vertex, with a specified binding index, and an explicit stride (which is not checked).
		/// </summary>
		/// <param name="binding">The binding index of the backing buffer.</param>
		/// <param name="stride">The explicit stride of the vertex data.</param>
		/// <param name="elems">The elements of the vertex to describe.</param>
		public VertexBinding(uint binding, uint stride, params VertexElement[] elems)
		{
			Stride = stride;
			Binding = binding;
			Elements = new VertexElement[elems.Length];
			Array.Copy(elems, Elements, elems.Length);
			PerInstance = false;
		}

		/// <summary>
		/// Describes a new vertex, with a specified binding index, and an explicit stride (which is not checked).
		/// </summary>
		/// <param name="binding">The binding index of the backing buffer.</param>
		/// <param name="perInstance">If this data is per-instance instead of per-vertex.</param>
		/// <param name="stride">The explicit stride of the vertex data.</param>
		/// <param name="elems">The elements of the vertex to describe.</param>
		public VertexBinding(uint binding, bool perInstance, uint stride, params VertexElement[] elems)
		{
			Stride = stride;
			Binding = binding;
			Elements = new VertexElement[elems.Length];
			Array.Copy(elems, Elements, elems.Length);
			PerInstance = perInstance;
		}

		/// <summary>
		/// Describes a new vertex, with a binding index of zero, calculating the stride, element locations, and element
		/// offsets assuming tight packing in both buffer data and shader binding slots.
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
			Binding = 0;
			Elements = elements;
			PerInstance = false;
		}

		/// <summary>
		/// Describes a new vertex, with a specified binding index, calculating the stride, element locations, and element
		/// offsets assuming tight packing in both buffer data and shader binding slots.
		/// </summary>
		/// <param name="binding">The binding index of the backing buffer.</param>
		/// <param name="fmts">The elements of the vertex, in order.</param>
		public VertexBinding(uint binding, params VertexElementFormat[] fmts)
		{
			uint loc = 0, off = 0;
			var elements = new VertexElement[fmts.Length];
			fmts.ForEach((fmt, idx) => {
				elements[idx] = new VertexElement(loc, fmt, off);
				loc += fmt.GetBindingSize();
				off += fmt.GetSize();
			});

			Stride = off;
			Binding = 0;
			Elements = elements;
			PerInstance = false;
		}

		/// <summary>
		/// Describes a new vertex, with a specified binding index, calculating the stride, element locations, and element
		/// offsets assuming tight packing in both buffer data and shader binding slots.
		/// </summary>
		/// <param name="binding">The binding index of the backing buffer.</param>
		/// <param name="perInstance">If this data is per-instance instead of per-vertex.</param>
		/// <param name="fmts">The elements of the vertex, in order.</param>
		public VertexBinding(uint binding, bool perInstance, params VertexElementFormat[] fmts)
		{
			uint loc = 0, off = 0;
			var elements = new VertexElement[fmts.Length];
			fmts.ForEach((fmt, idx) => {
				elements[idx] = new VertexElement(loc, fmt, off);
				loc += fmt.GetBindingSize();
				off += fmt.GetSize();
			});

			Stride = off;
			Binding = 0;
			Elements = elements;
			PerInstance = perInstance;
		}
		#endregion // Constructors

		private VertexBinding(uint s, uint b, VertexElement[] e, bool pi)
		{
			Stride = s;
			Binding = b;
			Elements = e;
			PerInstance = pi;
		}

		/// <summary>
		/// Creates a copy of this vertex binding, with an optionally different binding index.
		/// </summary>
		/// <param name="binding">The new binding index to use, or <see cref="UInt32.MaxValue"/> to keep the same index.</param>
		/// <returns>An identical vertex layout with a potentially new binding index.</returns>
		public VertexBinding Copy(uint binding = UInt32.MaxValue)
		{
			var elems = new VertexElement[Elements.Length];
			Array.Copy(Elements, elems, Elements.Length);
			return new VertexBinding(Stride, (binding == UInt32.MaxValue) ? Binding : binding, elems, PerInstance);
		}

		public override int GetHashCode() => (int)(Stride ^ (Binding << 9) ^ (Elements.Length << 18)); // Not ideal

		public override bool Equals(object obj) => (obj is VertexBinding) && (((VertexBinding)obj) == this);

		bool IEquatable<VertexBinding>.Equals(VertexBinding other) =>
			other.Stride == Stride && other.Binding == Binding && other.PerInstance == PerInstance && other.Elements.SequenceEqual(Elements);

		public static bool operator == (in VertexBinding l, in VertexBinding r) =>
			l.Stride == r.Stride && l.Binding == r.Binding && l.PerInstance == r.PerInstance && l.Elements.SequenceEqual(r.Elements);

		public static bool operator != (in VertexBinding l, in VertexBinding r) =>
			l.Stride != r.Stride || l.Binding != r.Binding || l.PerInstance != r.PerInstance || !l.Elements.SequenceEqual(r.Elements);
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
