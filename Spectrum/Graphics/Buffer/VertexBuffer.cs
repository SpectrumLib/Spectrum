/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2018-2019 The Spectrum Team
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */
using System;
using System.Runtime.InteropServices;
using Vk = SharpVk;

namespace Spectrum.Graphics
{
	/// <summary>
	/// A <see cref="Buffer"/> type specialized to hold vertex element data for rendering.
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
		/// Uploads vertex data into the buffer.
		/// </summary>
		/// <typeparam name="T">The type of the input data.</typeparam>
		/// <param name="data">The data to copy into the buffer.</param>
		/// <param name="dstOffset">The optional offset into the buffer, in verticies.</param>
		/// <param name="safe">Enacts additional checks for alignment to vertex boundaries in the buffer.</param>
		public void SetData<T>(ReadOnlySpan<T> data, uint dstOffset = 0, bool safe = true)
			where T : struct
		{
			var rawdata = MemoryMarshal.AsBytes(data);

			if (rawdata.Length > ((VertexCount - dstOffset) * Stride))
				throw new ArgumentException("Source data too large for vertex buffer.");
			if (safe && (rawdata.Length % Stride) != 0)
				throw new ArgumentException("Source data length is not aligned to a vertex boundary.");

			SetDataInternal(rawdata, dstOffset * Stride);
		}
	}
}
