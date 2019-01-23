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
	/// <remarks>
	/// Note that <see cref="VertexElement"/> values can be either complete or incomplete. When passing elements to
	/// create a new binding, all elements must be either complete or incomplete, no mixing is allowed. Mixing is not
	/// detected, and will not be corrected for, leading to potential severe rendering issues.
	/// </remarks>
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
			Elements = CompleteElements(elems);
			Stride = Elements.Max(e => e.Offset + e.Format.GetSize(e.Count));
			PerInstance = false;
		}

		/// <summary>
		/// Describes a new vertex, calculating the correct stride.
		/// </summary>
		/// <param name="perInstance">If this data is per-instance instead of per-vertex.</param>
		/// <param name="elems">The elements of the vertex to describe.</param>
		public VertexBinding(bool perInstance, params VertexElement[] elems)
		{
			Elements = CompleteElements(elems);
			Stride = Elements.Max(e => e.Offset + e.Format.GetSize(e.Count));
			PerInstance = perInstance;
		}

		/// <summary>
		/// Describes a new vertex, calculating the stride, element locations, and element offsets assuming tight 
		/// packing in both buffer data and shader binding slots.
		/// </summary>
		/// <param name="fmts">The elements of the vertex, in order.</param>
		public VertexBinding(params VertexElementFormat[] fmts)
		{
			Elements = PackFormats(fmts.Select(fmt => (fmt, 1u)).ToArray());
			Stride = Elements.Max(e => e.Offset + e.Format.GetSize(e.Count));
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
			Elements = PackFormats(fmts.Select(fmt => (fmt, 1u)).ToArray());
			Stride = Elements.Max(e => e.Offset + e.Format.GetSize(e.Count));
			PerInstance = perInstance;
		}

		/// <summary>
		/// Describes a new vertex, calculating the stride, element locations, and element offsets assuming tight 
		/// packing in both buffer data and shader binding slots.
		/// </summary>
		/// <param name="fmts">The elements of the vertex, in order.</param>
		public VertexBinding(params (VertexElementFormat, uint)[] fmts)
		{
			Elements = PackFormats(fmts);
			Stride = Elements.Max(e => e.Offset + e.Format.GetSize(e.Count));
			PerInstance = false;
		}

		/// <summary>
		/// Describes a new vertex, calculating the stride, element locations, and element offsets assuming tight 
		/// packing in both buffer data and shader binding slots.
		/// </summary>
		/// <param name="perInstance">If this data is per-instance instead of per-vertex.</param>
		/// <param name="fmts">The elements of the vertex, in order.</param>
		public VertexBinding(bool perInstance, params (VertexElementFormat, uint)[] fmts)
		{
			Elements = PackFormats(fmts);
			Stride = Elements.Max(e => e.Offset + e.Format.GetSize(e.Count));
			PerInstance = perInstance;
		}
		#endregion // Constructors

		private VertexBinding(uint stride, bool perInstance, VertexElement[] elems)
		{
			Stride = stride;
			Elements = new VertexElement[elems.Length];
			Array.Copy(elems, Elements, elems.Length);
			PerInstance = perInstance;
		}

		/// <summary>
		/// Creates a copy of this vertex binding.
		/// </summary>
		/// <returns>An identical vertex layout.</returns>
		public VertexBinding Copy() => new VertexBinding(Stride, PerInstance, Elements);

		public override int GetHashCode() => (int)(~(Stride * 55009) ^ (Elements.Length << 18)); // Really not ideal

		public override bool Equals(object obj) => (obj is VertexBinding) && (((VertexBinding)obj) == this);

		bool IEquatable<VertexBinding>.Equals(VertexBinding other) =>
			other.Stride == Stride && other.PerInstance == PerInstance && other.Elements.SequenceEqual(Elements);

		public static bool operator == (in VertexBinding l, in VertexBinding r) =>
			l.Stride == r.Stride && l.PerInstance == r.PerInstance && l.Elements.SequenceEqual(r.Elements);

		public static bool operator != (in VertexBinding l, in VertexBinding r) =>
			l.Stride != r.Stride || l.PerInstance != r.PerInstance || !l.Elements.SequenceEqual(r.Elements);

		// For incomplete elements, this will complete them into a new array, otherwise it will just copy the array
		private static VertexElement[] CompleteElements(VertexElement[] elems)
		{
			var elements = new VertexElement[elems.Length];
			if (elems[0].IsComplete)
				Array.Copy(elems, elements, elems.Length);
			else
			{
				uint loc = 0, off = 0;
				elems.ForEach((el, idx) => {
					elements[idx] = new VertexElement(loc, el.Format, off);
					loc += el.Format.GetBindingSize(el.Count);
					off += el.Format.GetSize(el.Count);
				});
			}
			return elements;
		}

		// This will build a tightly-packed vertex element array from formats
		private static VertexElement[] PackFormats(in (VertexElementFormat fmt, uint count)[] fmts)
		{
			var elements = new VertexElement[fmts.Length];
			uint loc = 0, off = 0;
			fmts.ForEach((fmt, idx) => {
				elements[idx] = new VertexElement(loc, fmt.fmt, off);
				loc += fmt.fmt.GetBindingSize(fmt.count);
				off += fmt.fmt.GetSize(fmt.count);
			});
			return elements;
		}
	}

	/// <summary>
	/// Describes a single element of a vertex. 
	/// </summary>
	/// <remarks>
	/// Based on the constructor used, this type can have complete values, which have all members fully defined, or
	/// incomplete values, which do not have their location and offset defined. The incomplete values are not valid
	/// until they are passed to a <see cref="VertexBinding"/> constructor, which will calculate their missing values
	/// to make them complete. Additionally, complete and incomplete values cannot be mixed when being used to build 
	/// a <see cref="VertexBinding"/> object. See <see cref="VertexElement.IsComplete"/>.
	/// </remarks>
	public struct VertexElement : IEquatable<VertexElement>
	{
		#region Fields
		/// <summary>
		/// The location of the vertex element (in the layout specifier in GLSL).
		/// </summary>
		public readonly uint Location;
		/// <summary>
		/// The offset (in bytes) of the element into a vertex in the backing buffer.
		/// </summary>
		public readonly uint Offset;
		/// <summary>
		/// The format of the element.
		/// </summary>
		public readonly VertexElementFormat Format;
		/// <summary>
		/// If the vertex element is an array, this is the number of elements in the array.
		/// </summary>
		public readonly uint Count;

		/// <summary>
		/// If this element is complete, meaning all fields are explicity defined. Incomplete elements can be passed to
		/// <see cref="VertexBinding"/>s to autocomplete their values.
		/// </summary>
		public bool IsComplete => (Location != UInt32.MaxValue) && (Offset != UInt32.MaxValue);
		#endregion // Fields

		/// <summary>
		/// Describes a new vertex element with the passed values, which will be complete.
		/// </summary>
		/// <param name="location">The location of the element in the vertex shader.</param>
		/// <param name="format">The format of the element.</param>
		/// <param name="offset">The offset (in bytes) of the element into a vertex in the backing buffer.</param>
		/// <param name="count">If the vertex element is an array, this is the number of elements in the array.</param>
		public VertexElement(uint location, VertexElementFormat format, uint offset, uint count = 1)
		{
			Location = location;
			Offset = offset;
			Format = format;
			Count = count;
		}

		/// <summary>
		/// Describes an incomplete element, whose <see cref="Location"/> and <see cref="Offset"/> fields will be
		/// calculated and set automatically when added to a <see cref="VertexBinding"/>.
		/// </summary>
		/// <param name="format">The format of the element.</param>
		/// <param name="count">If the vertex element is an array, this is the number of elements in the array.</param>
		public VertexElement(VertexElementFormat format, uint count = 1)
		{
			Location = UInt32.MaxValue;
			Offset = UInt32.MaxValue;
			Format = format;
			Count = count;
		}

		/// <summary>
		/// Creates a new element based off of an existing element, with the option to mutate the location and offset
		/// of the element.
		/// </summary>
		/// <param name="elem">The existing element to use.</param>
		/// <param name="location">An optional new value for the element location.</param>
		/// <param name="offset">An optional new value for the element offset.</param>
		public VertexElement(in VertexElement elem, uint? location = null, uint? offset = null)
		{
			Location = location.HasValue ? location.Value : elem.Location;
			Offset = offset.HasValue ? offset.Value : elem.Offset;
			Format = elem.Format;
			Count = elem.Count;
		}

		public override string ToString() => $"{{{Format}{(Count > 1 ? $"[{Count}]" : "")} {Location}:{Offset}}}";

		public override int GetHashCode() => (int)(Location ^ (Offset << 9) ^ ((int)Format << 18) ^ ((int)Count << 27));

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public override bool Equals(object obj) => (obj is VertexElement) && (((VertexElement)obj) == this);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		bool IEquatable<VertexElement>.Equals(VertexElement other) =>
			other.Location == Location && other.Offset == Offset && other.Format == Format && other.Count == Count;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool operator == (in VertexElement l, in VertexElement r) =>
			l.Location == r.Location && l.Offset == r.Offset && l.Format == r.Format && l.Count == r.Count;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool operator != (in VertexElement l, in VertexElement r) =>
			l.Location != r.Location || l.Offset != r.Offset || l.Format != r.Format || l.Count != r.Count;

		/// <summary>
		/// Creates an incomplete element, assuming it is not an array.
		/// </summary>
		/// <param name="fmt">The format of the element.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static implicit operator VertexElement (VertexElementFormat fmt) => new VertexElement(fmt, 1);

		/// <summary>
		/// Creates an incomplete element, complete with an array size specifier.
		/// </summary>
		/// <param name="fmt">The format and array size of the element.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static implicit operator VertexElement (in (VertexElementFormat, uint) fmt) => new VertexElement(fmt.Item1, fmt.Item2);
	}
}
