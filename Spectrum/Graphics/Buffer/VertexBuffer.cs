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
	/// A <see cref="Buffer"/> type that is specialized to be used as a source of vertex attribute data for input into
	/// shaders.
	/// </summary>
	public sealed class VertexBuffer : Buffer
	{
		#region Fields
		/// <summary>
		/// The vertex layout that is used by this buffer.
		/// </summary>
		public readonly VertexBinding Binding;
		/// <summary>
		/// The size of a single vertex in this buffer.
		/// </summary>
		public uint Stride => Binding.Stride;
		/// <summary>
		/// The number of vertices stored in this buffer.
		/// </summary>
		public readonly uint VertexCount;
		#endregion // Fields

		/// <summary>
		/// Creates a new vertex buffer to hold the given vertex type.
		/// </summary>
		/// <param name="binding">The layout of the vertex type held in the buffer.</param>
		/// <param name="count">The number of vertices in the buffer.</param>
		public VertexBuffer(VertexBinding binding, uint count) :
			base(binding.Stride * count, BufferType.Vertex, Vk.BufferUsageFlags.VertexBuffer)
		{
			Binding = binding.Copy();
			VertexCount = count;
		}

		/// <summary>
		/// Uploads vertex data into the buffer on the graphics device.
		/// </summary>
		/// <typeparam name="T">The type of the input data.</typeparam>
		/// <param name="data">The data to copy into the buffer.</param>
		/// <param name="length">The number of items to copy from the array.</param>
		/// <param name="srcOffset">The index of the source array to start copying from.</param>
		/// <param name="dstOffset">The optional offset into the buffer, in vertices.</param>
		public void SetData<T>(T[] data, uint length, uint srcOffset, uint dstOffset)
			where T : struct
		{
			if ((length + srcOffset) > data.Length)
				throw new ArgumentException($"Source data too short ({data.Length}) for offset and length ({srcOffset}:{length})");

			uint typeSize = (uint)Unsafe.SizeOf<T>();
			uint srcSize = length * typeSize;
			uint srcOff = srcOffset * typeSize;

			if ((srcOff % Stride) != 0)
				throw new ArgumentException($"Source offset ({srcOff}) misalignment to vertex boundary ({Stride})");
			if ((srcSize % Stride) != 0)
				throw new ArgumentException($"Source size ({srcSize}) misalignment to vertex boundary ({Stride})");

			SetDataInternal(new ReadOnlySpan<T>(data, (int)srcOffset, (int)length), dstOffset * Stride);
		}

		/// <summary>
		/// Uploads vertex data into the buffer on the graphics device.
		/// </summary>
		/// <typeparam name="T">The type of the input data.</typeparam>
		/// <param name="data">The data to copy into the buffer.</param>
		/// <param name="dstOffset">The optional offset into the buffer, in vertices.</param>
		public void SetData<T>(ReadOnlySpan<T> data, uint dstOffset)
			where T : struct
		{
			uint typeSize = (uint)Unsafe.SizeOf<T>();
			uint srcSize = (uint)data.Length * typeSize;

			if ((srcSize % Stride) != 0)
				throw new ArgumentException($"Source size ({srcSize}) misalignment to vertex boundary ({Stride})");

			SetDataInternal(data, dstOffset * Stride);
		}
	}
}
