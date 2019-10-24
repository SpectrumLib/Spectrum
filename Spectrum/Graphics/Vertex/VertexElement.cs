/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2018-2019 The Spectrum Team
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */
using System;
using System.Runtime.CompilerServices;
using Vk = SharpVk;

namespace Spectrum.Graphics
{
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
	public readonly struct VertexElement : IEquatable<VertexElement>
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
		public readonly bool IsComplete => (Location != UInt32.MaxValue) && (Offset != UInt32.MaxValue);
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

		public readonly override string ToString() => $"{{{Format}{(Count > 1 ? $"[{Count}]" : "")} {Location}:{Offset}}}";

		public readonly override int GetHashCode() => (int)(Location ^ (Offset << 9) ^ ((int)Format << 18) ^ ((int)Count << 27));

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public readonly override bool Equals(object obj) => (obj is VertexElement) && (((VertexElement)obj) == this);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		readonly bool IEquatable<VertexElement>.Equals(VertexElement other) =>
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

	/// <summary>
	/// The various data formats that vertex elements can take on.
	/// </summary>
	public enum VertexElementFormat
	{
		/// <summary>
		/// A single-precision floating point number (GLSL `float`).
		/// </summary>
		Float = Vk.Format.R32SFloat,
		/// <summary>
		/// A 2-component single-precision floating point vector (GLSL `vec2`).
		/// </summary>
		Float2 = Vk.Format.R32G32SFloat,
		/// <summary>
		/// A 3-component single-precision floating point vector (GLSL `vec3`).
		/// </summary>
		Float3 = Vk.Format.R32G32B32SFloat,
		/// <summary>
		/// A 4-component single-precision floating point vector (GLSL `vec4`).
		/// </summary>
		Float4 = Vk.Format.R32G32B32A32SFloat,

		/// <summary>
		/// A single 32-bit signed integer (GLSL `int`).
		/// </summary>
		Int = Vk.Format.R32SInt,
		/// <summary>
		/// A 2-component 32-bit signed integer vector (GLSL `ivec2`).
		/// </summary>
		Int2 = Vk.Format.R32G32SInt,
		/// <summary>
		/// A 3-component 32-bit signed integer vector (GLSL `ivec3`).
		/// </summary>
		Int3 = Vk.Format.R32G32B32SInt,
		/// <summary>
		/// A 4-component 32-bit signed integer vector (GLSL `ivec4`).
		/// </summary>
		Int4 = Vk.Format.R32G32B32A32SInt,

		/// <summary>
		/// A single 32-bit signed integer (GLSL `uint`).
		/// </summary>
		UInt = Vk.Format.R32UInt,
		/// <summary>
		/// A 2-component 32-bit signed integer vector (GLSL `uvec2`).
		/// </summary>
		UInt2 = Vk.Format.R32G32UInt,
		/// <summary>
		/// A 3-component 32-bit signed integer vector (GLSL `uvec3`).
		/// </summary>
		UInt3 = Vk.Format.R32G32B32UInt,
		/// <summary>
		/// A 4-component 32-bit signed integer vector (GLSL `uvec4`).
		/// </summary>
		UInt4 = Vk.Format.R32G32B32A32UInt
	}

	/// <summary>
	/// Contains extension methods for working with <see cref="VertexElementFormat"/> values.
	/// </summary>
	public static class VertexElementFormatExtensions
	{
		/// <summary>
		/// Gets the size, in bytes, of the element format in memory.
		/// </summary>
		/// <param name="fmt">The format to get the size for.</param>
		/// <param name="arrSize">The number of elements in the array, if the type is an array.</param>
		/// <returns>The element memory footprint, in bytes.</returns>
		public static uint GetSize(this VertexElementFormat fmt, uint arrSize = 1)
		{
			uint sz = 0;
			switch (fmt)
			{
				case VertexElementFormat.Float:
				case VertexElementFormat.Int:
				case VertexElementFormat.UInt:
					sz = 4; break;
				case VertexElementFormat.Float2:
				case VertexElementFormat.Int2:
				case VertexElementFormat.UInt2:
					sz = 8; break;
				case VertexElementFormat.Float3:
				case VertexElementFormat.Int3:
				case VertexElementFormat.UInt3:
					sz = 12; break;
				case VertexElementFormat.Float4:
				case VertexElementFormat.Int4:
				case VertexElementFormat.UInt4:
					sz = 16; break;
				default:
					throw new ArgumentOutOfRangeException(nameof(fmt), "Cannot get the size of an invalid vertex element format.");
			}
			return sz * arrSize;
		}

		/// <summary>
		/// Gets the size of the format in terms of binding slots in a shader.
		/// </summary>
		/// <param name="fmt">The format to get the size for.</param>
		/// <param name="arrSize">The number of elements in the array, if the type is an array.</param>
		/// <returns>The size of the format, in shader binding slots.</returns>
		/// <remarks>A shader binding slot is 16 bytes on basically all hardware.</remarks>
		public static uint GetBindingSize(this VertexElementFormat fmt, uint arrSize = 1) => (uint)Math.Ceiling(GetSize(fmt, arrSize) / 16.0f);
	}
}
