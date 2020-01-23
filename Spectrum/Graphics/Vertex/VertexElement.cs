/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2018-2020 The Spectrum Team
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */
using System;
using System.Runtime.CompilerServices;
using Vk = SharpVk;

namespace Spectrum.Graphics
{
	/// <summary>
	/// Describes a single element in a vertex. 
	/// </summary>
	public struct VertexElement : IEquatable<VertexElement>
	{
		#region Fields
		/// <summary>
		/// The location of the vertex element (the <c>attr</c> index in HLSV).
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
		/// <summary>
		/// If the vertex element is an array, this is the number of elements in the array.
		/// </summary>
		public uint? ArraySize;
		#endregion // Fields

		/// <summary>
		/// Describes a new vertex element with the passed values, which will be complete.
		/// </summary>
		/// <param name="format">The format of the element.</param>
		/// <param name="location">The location of the element in the vertex shader.</param>
		/// <param name="offset">The offset (in bytes) of the element into a vertex in the backing buffer.</param>
		/// <param name="asize">If the vertex element is an array, this is the number of elements in the array.</param>
		public VertexElement(VertexElementFormat format, uint location, uint offset, uint? asize = null)
		{
			Location = location;
			Offset = offset;
			Format = format;
			ArraySize = asize;
		}

		#region Overrides
		public readonly override string ToString() => $"{{{Format}{(ArraySize.HasValue ? $"[{ArraySize}]" : "")} {Location}:{Offset}}}";

		public readonly override int GetHashCode() => (int)(Location ^ (Offset << 8) ^ ((int)Format << 16) ^ ((int)ArraySize.GetValueOrDefault(1) << 24));

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public readonly override bool Equals(object obj) => (obj is VertexElement) && (((VertexElement)obj) == this);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		readonly bool IEquatable<VertexElement>.Equals(VertexElement other) =>
			other.Location == Location && other.Offset == Offset && other.Format == Format && other.ArraySize == ArraySize;
		#endregion // Overrides

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool operator == (in VertexElement l, in VertexElement r) =>
			l.Location == r.Location && l.Offset == r.Offset && l.Format == r.Format && l.ArraySize == r.ArraySize;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool operator != (in VertexElement l, in VertexElement r) =>
			l.Location != r.Location || l.Offset != r.Offset || l.Format != r.Format || l.ArraySize != r.ArraySize;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static implicit operator VertexElement (in (VertexElementFormat, uint, uint) tup) =>
			new VertexElement(tup.Item1, tup.Item2, tup.Item3);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static implicit operator VertexElement (in (VertexElementFormat, uint, uint, uint) tup) =>
			new VertexElement(tup.Item1, tup.Item2, tup.Item3, tup.Item4);
	}

	/// <summary>
	/// The various data formats that vertex elements can take on.
	/// </summary>
	public enum VertexElementFormat
	{
		/// <summary>
		/// A single-precision floating point number (HLSV `float`).
		/// </summary>
		Float = Vk.Format.R32SFloat,
		/// <summary>
		/// A 2-component single-precision floating point vector (HLSV `vec2`).
		/// </summary>
		Float2 = Vk.Format.R32G32SFloat,
		/// <summary>
		/// A 3-component single-precision floating point vector (HLSV `vec3`).
		/// </summary>
		Float3 = Vk.Format.R32G32B32SFloat,
		/// <summary>
		/// A 4-component single-precision floating point vector (HLSV `vec4`).
		/// </summary>
		Float4 = Vk.Format.R32G32B32A32SFloat,

		/// <summary>
		/// A single 32-bit signed integer (HLSV `int`).
		/// </summary>
		Int = Vk.Format.R32SInt,
		/// <summary>
		/// A 2-component 32-bit signed integer vector (HLSV `ivec2`).
		/// </summary>
		Int2 = Vk.Format.R32G32SInt,
		/// <summary>
		/// A 3-component 32-bit signed integer vector (HLSV `ivec3`).
		/// </summary>
		Int3 = Vk.Format.R32G32B32SInt,
		/// <summary>
		/// A 4-component 32-bit signed integer vector (HLSV `ivec4`).
		/// </summary>
		Int4 = Vk.Format.R32G32B32A32SInt,

		/// <summary>
		/// A single 32-bit signed integer (HLSV `uint`).
		/// </summary>
		UInt = Vk.Format.R32UInt,
		/// <summary>
		/// A 2-component 32-bit signed integer vector (HLSV `uvec2`).
		/// </summary>
		UInt2 = Vk.Format.R32G32UInt,
		/// <summary>
		/// A 3-component 32-bit signed integer vector (HLSV `uvec3`).
		/// </summary>
		UInt3 = Vk.Format.R32G32B32UInt,
		/// <summary>
		/// A 4-component 32-bit signed integer vector (HLSV `uvec4`).
		/// </summary>
		UInt4 = Vk.Format.R32G32B32A32UInt,

		/// <summary>
		/// A 4-component 8-bit normalized unsigned integer vector. This is the correct layout for 
		/// <see cref="Spectrum.Color"/>.
		/// </summary>
		Color = Vk.Format.R8G8B8A8UNorm
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
		/// <returns>The element memory footprint, in bytes.</returns>
		public static uint GetSize(this VertexElementFormat fmt)
		{
			switch (fmt)
			{
				case VertexElementFormat.Float:
				case VertexElementFormat.Int:
				case VertexElementFormat.UInt:
				case VertexElementFormat.Color:
					return 4;
				case VertexElementFormat.Float2:
				case VertexElementFormat.Int2:
				case VertexElementFormat.UInt2:
					return 8;
				case VertexElementFormat.Float3:
				case VertexElementFormat.Int3:
				case VertexElementFormat.UInt3:
					return 12;
				case VertexElementFormat.Float4:
				case VertexElementFormat.Int4:
				case VertexElementFormat.UInt4:
					return 16;
				default:
					throw new ArgumentOutOfRangeException(nameof(fmt), "GetSize() - Invalid vertex element format.");
			}
		}
	}
}
