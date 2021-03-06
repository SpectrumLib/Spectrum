﻿using System;
using Vk = VulkanCore;

namespace Spectrum.Graphics
{
	/// <summary>
	/// A <see cref="Buffer"/> type that is specialized and used as a source of vertex indices when using indexed
	/// rendering.
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
			base(count * ((type == IndexElementType.U16) ? 2u : 4u), BufferType.Index, Vk.BufferUsages.IndexBuffer)
		{
			ElementType = type;
			IndexCount = count;
		}

		/// <summary>
		/// Uploads unsigned 16-bit index data into the buffer on the graphics device. If the indicies in this buffer
		/// are not 16-bit, an exception is thrown.
		/// </summary>
		/// <param name="indices">The indices to upload to the buffer.</param>
		/// <param name="length">
		/// The number of indices to copy. A value of <see cref="UInt32.MaxValue"/> will auto-calculate the proper 
		/// length to fill the buffer, taking into account the offset into the buffer.
		/// </param>
		/// <param name="srcOffset">The offset into the source array, in array indices.</param>
		/// <param name="dstOffset">The offset into the buffer, in index elements.</param>
		public void SetData(ushort[] indices, uint length = UInt32.MaxValue, uint srcOffset = 0, uint dstOffset = 0)
		{
			if (ElementType != IndexElementType.U16)
				throw new InvalidOperationException("Cannot upload 16-bit indices to a 32-bit index buffer");

			if (length == UInt32.MaxValue)
				length = IndexCount - dstOffset;

			SetDataInternal(indices, length, srcOffset, dstOffset * 2);
		}

		/// <summary>
		/// Uploads unsigned 32-bit index data into the buffer on the graphics device. If the indicies in this buffer
		/// are not 32-bit, an exception is thrown.
		/// </summary>
		/// <param name="indices">The indices to upload to the buffer.</param>
		/// <param name="length">
		/// The number of indices to copy. A value of <see cref="UInt32.MaxValue"/> will auto-calculate the proper 
		/// length to fill the buffer, taking into account the offset into the buffer.
		/// </param>
		/// <param name="srcOffset">The offset into the source array, in array indices.</param>
		/// <param name="dstOffset">The offset into the buffer, in index elements.</param>
		public void SetData(uint[] indices, uint length = UInt32.MaxValue, uint srcOffset = 0, uint dstOffset = 0)
		{
			if (ElementType != IndexElementType.U32)
				throw new InvalidOperationException("Cannot upload 32-bit indices to a 16-bit index buffer");

			if (length == UInt32.MaxValue)
				length = IndexCount - dstOffset;

			SetDataInternal(indices, length, srcOffset, dstOffset * 4);
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
		U16 = Vk.IndexType.UInt16,
		/// <summary>
		/// The index elements are unsigned 32-bit integers.
		/// </summary>
		U32 = Vk.IndexType.UInt32
	}
}
