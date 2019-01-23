using System;
using Vk = VulkanCore;

namespace Spectrum.Graphics
{
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
		/// A single 32-bit signed integer (GLSL `int`).
		/// </summary>
		UInt = Vk.Format.R32UInt,
		/// <summary>
		/// A 2-component 32-bit signed integer vector (GLSL `ivec2`).
		/// </summary>
		UInt2 = Vk.Format.R32G32UInt,
		/// <summary>
		/// A 3-component 32-bit signed integer vector (GLSL `ivec3`).
		/// </summary>
		UInt3 = Vk.Format.R32G32B32UInt,
		/// <summary>
		/// A 4-component 32-bit signed integer vector (GLSL `ivec4`).
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
		public static uint GetBindingSize(this VertexElementFormat fmt, uint arrSize = 1) => (uint)Mathf.Ceiling(GetSize(fmt, arrSize) / 16.0f);
	}
}
