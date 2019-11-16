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
	/// A <see cref="Buffer"/> type specialized to hold index data for indexed rendering.
	/// </summary>
	public sealed class IndexBuffer : Buffer
	{
		#region Fields
		/// <summary>
		/// The type of the index elements.
		/// </summary>
		public readonly IndexElementType ElementType;
		/// <summary>
		/// The number of indices in this buffer.
		/// </summary>
		public readonly uint IndexCount;
		#endregion // Fields

		/// <summary>
		/// Creates a new index buffer to hold the given index type.
		/// </summary>
		/// <param name="type">The type of the index elements.</param>
		/// <param name="count">The number of indices for this buffer to hold.</param>
		public IndexBuffer(IndexElementType type, uint count) :
			base(count * ((type == IndexElementType.U16) ? 2u : 4u), BufferType.Index, Vk.BufferUsageFlags.IndexBuffer)
		{
			ElementType = type;
			IndexCount = count;
		}

		/// <summary>
		/// Uploads unsigned 16-bit index data into the buffer.
		/// </summary>
		/// <param name="indices">The indices to upload to the buffer.</param>
		/// <param name="dstOffset">The offset into the buffer, in index elements.</param>
		/// <exception cref="InvalidOperationException">The buffer holds 32-bit indices.</exception>
		public void SetData(ReadOnlySpan<ushort> indices, uint dstOffset = 0)
		{
			if (ElementType != IndexElementType.U16)
				throw new InvalidOperationException("Upload 16-bit indices to a 32-bit index buffer.");
			if (indices.Length > (IndexCount - dstOffset))
				throw new ArgumentException("Source data too large for index buffer.");

			SetDataInternal(MemoryMarshal.AsBytes(indices), dstOffset * 2);
		}

		/// <summary>
		/// Uploads unsigned 32-bit index data into the buffer.
		/// </summary>
		/// <param name="indices">The indices to upload to the buffer.</param>
		/// <param name="dstOffset">The offset into the buffer, in index elements.</param>
		/// <exception cref="InvalidOperationException">The buffer holds 16-bit indices.</exception>
		public void SetData(ReadOnlySpan<uint> indices, uint dstOffset = 0)
		{
			if (ElementType != IndexElementType.U32)
				throw new InvalidOperationException("Upload 32-bit indices to a 16-bit index buffer");
			if (indices.Length > (IndexCount - dstOffset))
				throw new ArgumentException("Source data too large for index buffer.");

			SetDataInternal(MemoryMarshal.AsBytes(indices), dstOffset * 4);
		}
	}

	/// <summary>
	/// The types for index elements.
	/// </summary>
	public enum IndexElementType
	{
		/// <summary>
		/// The index elements are unsigned 16-bit integers.
		/// </summary>
		U16 = Vk.IndexType.Uint16,
		/// <summary>
		/// The index elements are unsigned 32-bit integers.
		/// </summary>
		U32 = Vk.IndexType.Uint32
	}
}
