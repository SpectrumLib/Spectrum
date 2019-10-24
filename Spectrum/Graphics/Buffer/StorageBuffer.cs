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
	/// A <see cref="Buffer"/> type that is specialized to store general structured memory, which shaders have read and
	/// store access to.
	/// </summary>
	public sealed class StorageBuffer : Buffer
	{
		/// <summary>
		/// Creates a new general-use storage buffer of the given size.
		/// </summary>
		/// <param name="size">The size of the buffer, in bytes.</param>
		public StorageBuffer(uint size) :
			base(size, BufferType.Storage, Vk.BufferUsageFlags.StorageBuffer)
		{ }

		/// <summary>
		/// Uploads general structured data into the buffer on the graphics card.
		/// </summary>
		/// <typeparam name="T">The type of the source data.</typeparam>
		/// <param name="data">The data to upload.</param>
		/// <param name="length">The number of array elements to copy.</param>
		/// <param name="srcOffset">The offset into the source array to start copying from.</param>
		/// <param name="dstOffset">The offset into the buffer copy to, in <typeparamref name="T"/> elements.</param>
		public void SetData<T>(T[] data, uint length, uint srcOffset, uint dstOffset)
			where T : struct
		{
			if ((length + srcOffset) > data.Length)
				throw new ArgumentException($"Source data too short ({data.Length}) for offset and length ({srcOffset}:{length})");

			SetDataInternal(new ReadOnlySpan<T>(data, (int)srcOffset, (int)length), dstOffset * (uint)Unsafe.SizeOf<T>());
		}

		/// <summary>
		/// Uploads general structured data into the buffer on the graphics card.
		/// </summary>
		/// <typeparam name="T">The type of the source data.</typeparam>
		/// <param name="data">The data to upload.</param>
		/// <param name="dstOffset">The offset into the buffer copy to, in <typeparamref name="T"/> elements.</param>
		public void SetData<T>(ReadOnlySpan<T> data, uint dstOffset)
			where T : struct =>
			SetDataInternal(data, dstOffset * (uint)Unsafe.SizeOf<T>());

		/// <summary>
		/// Pulls general structured data from the buffer.
		/// </summary>
		/// <typeparam name="T">The type of the destination data.</typeparam>
		/// <param name="data">The array to place the data into.</param>
		/// <param name="length">The number of array indices to copy from the buffer.</param>
		/// <param name="dstOffset">The index of the array to start copying into.</param>
		/// <param name="srcOffset">The optional offset into the buffer, in <typeparamref name="T"/>s.</param>
		public void GetData<T>(T[] data, uint length, uint dstOffset, uint srcOffset)
			where T : struct
		{
			if ((length + dstOffset) > data.Length)
				throw new ArgumentException($"Source data too short ({data.Length}) for offset and length ({dstOffset}:{length})");

			GetDataInternal(new Span<T>(data, (int)dstOffset, (int)length), srcOffset * (uint)Unsafe.SizeOf<T>());
		}

		/// <summary>
		/// Pulls general structured data from the buffer.
		/// </summary>
		/// <typeparam name="T">The type of the destination data.</typeparam>
		/// <param name="data">The array to place the data into.</param>
		/// <param name="srcOffset">The optional offset into the buffer, in <typeparamref name="T"/>s.</param>
		public void GetData<T>(Span<T> data, uint srcOffset)
			where T : struct =>
			GetDataInternal(data, srcOffset * (uint)Unsafe.SizeOf<T>());
	}
}
