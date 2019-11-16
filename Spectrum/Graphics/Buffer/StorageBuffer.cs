/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2018-2019 The Spectrum Team
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Vk = SharpVk;

namespace Spectrum.Graphics
{
	/// <summary>
	/// A <see cref="Buffer"/> type specialized to hold general structured memory, which shaders have read and store
	/// access to.
	/// </summary>
	public sealed class StorageBuffer : Buffer
	{
		/// <summary>
		/// Creates a new storage buffer of the given size.
		/// </summary>
		/// <param name="size">The size of the buffer, in bytes.</param>
		public StorageBuffer(uint size) :
			base(size, BufferType.Storage, Vk.BufferUsageFlags.TransferSource | Vk.BufferUsageFlags.StorageBuffer)
		{ }

		/// <summary>
		/// Uploads general structured data into the buffer.
		/// </summary>
		/// <param name="data">The raw data to upload.</param>
		/// <param name="dstOffset">The offset into the buffer, in bytes.</param>
		public void SetData(ReadOnlySpan<byte> data, uint dstOffset = 0)
		{
			if (data.Length > (Size - dstOffset))
				throw new ArgumentException("Source data too large for storage buffer.");

			SetDataInternal(data, dstOffset);
		}

		/// <summary>
		/// Uploads typed structured data into the buffer.
		/// </summary>
		/// <typeparam name="T">The structure type to upload.</typeparam>
		/// <param name="data">The data to upload.</param>
		/// <param name="dstOffset">The offset into the buffer, in <typeparamref name="T"/> strides.</param>
		public void SetData<T>(ReadOnlySpan<T> data, uint dstOffset = 0)
			where T : struct =>
			SetData(MemoryMarshal.AsBytes(data), dstOffset * (uint)Unsafe.SizeOf<T>());

		/// <summary>
		/// Copies general structured data from the buffer into a memory block.
		/// </summary>
		/// <param name="data">The memory to copy the data into.</param>
		/// <param name="srcOffset">The offset into the device buffer, in bytes.</param>
		public void GetData(Span<byte> data, uint srcOffset = 0)
		{
			if (data.Length > (Size - srcOffset))
				throw new ArgumentException("Storage buffer too small for requested amount of data.");

			GetDataInternal(data, srcOffset);
		}

		/// <summary>
		/// Copies typed structured data from the buffer into a memory block.
		/// </summary>
		/// <typeparam name="T">The structure type to copy.</typeparam>
		/// <param name="data">The memory to copy the data into.</param>
		/// <param name="srcOffset">The offset into the device buffer, in <typeparamref name="T"/> strides.</param>
		public void GetData<T>(Span<T> data, uint srcOffset = 0)
			where T : struct =>
			GetData(MemoryMarshal.AsBytes(data), srcOffset * (uint)Unsafe.SizeOf<T>());
	}
}
