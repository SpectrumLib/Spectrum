using System;
using System.Runtime.InteropServices;
using Vk = VulkanCore;

namespace Spectrum.Graphics
{
	/// <summary>
	/// A <see cref="Buffer"/> type that is specialized to store general structured memory, which shaders have read and
	/// store access to, as well as atomic operations on buffer members that are unsigned integers.
	/// </summary>
	public sealed class StorageBuffer : Buffer
	{
		/// <summary>
		/// Creates a new general-use storage buffer of the given size.
		/// </summary>
		/// <param name="size">The size of the buffer, in bytes.</param>
		public StorageBuffer(uint size) :
			base(size, BufferType.Storage, Vk.BufferUsages.StorageBuffer)
		{ }

		/// <summary>
		/// Uploads general structured data into the buffer on the graphics card.
		/// </summary>
		/// <typeparam name="T">The type of the source data.</typeparam>
		/// <param name="data">The data to upload.</param>
		/// <param name="length">
		/// The length of the source data to copy, in <typeparamref name="T"/>s. A value of <see cref="UInt32.MaxValue"/>
		/// will auto-calculate the proper length to fill the buffer, taking into account the offset into the buffer.
		/// </param>
		/// <param name="srcOffset">The optional offset into the source data, in <typeparamref name="T"/>s.</param>
		/// <param name="dstOffset">The optional offset into the buffer, in <typeparamref name="T"/>s.</param>
		public void SetData<T>(T[] data, uint length = UInt32.MaxValue, uint srcOffset = 0, uint dstOffset = 0)
			where T : struct
		{
			uint typeSize = (uint)Marshal.SizeOf<T>();
			if (length == UInt32.MaxValue)
				length = (uint)Mathf.Ceiling((Size - dstOffset) / (float)typeSize);

			SetDataInternal(data, length, srcOffset, dstOffset * typeSize);
		}

		/// <summary>
		/// Downloads general structured data from the buffer.
		/// </summary>
		/// <typeparam name="T">The type of the destination data.</typeparam>
		/// <param name="data">
		/// The array to place the data into, or null to autocreate an array of the correct size, taking into account
		/// the requested offset.
		/// </param>
		/// <param name="length">The length of the data to download, in <typeparamref name="T"/>s.</param>
		/// <param name="dstOffset">The optional offset into the source data, in <typeparamref name="T"/>s.</param>
		/// <param name="srcOffset">The optional offset into the buffer, in <typeparamref name="T"/>s.</param>
		public void GetData<T>(ref T[] data, uint length, uint dstOffset = 0, uint srcOffset = 0)
			where T : struct =>
			GetDataInternal(ref data, length, dstOffset, srcOffset * (uint)Marshal.SizeOf<T>());
	}
}
