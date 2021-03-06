﻿using System;
using System.Runtime.InteropServices;
using Vk = VulkanCore;

namespace Spectrum.Graphics
{
	/// <summary>
	/// A <see cref="Buffer"/> type that is specialized and used as a source of vertex attribute data for input into
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
			base(binding.Stride * count, BufferType.Vertex, Vk.BufferUsages.VertexBuffer)
		{
			Binding = binding.Copy();
			VertexCount = count;
		}

		/// <summary>
		/// Uploads vertex data into the buffer on the graphics device.
		/// </summary>
		/// <typeparam name="T">The type of the input data.</typeparam>
		/// <param name="data">The data to copy into the buffer.</param>
		/// <param name="length">
		/// The length of the source data to copy, in <typeparamref name="T"/>s. A value of <see cref="UInt32.MaxValue"/>
		/// will auto-calculate the proper length to fill the buffer, taking into account the offset into the buffer.
		/// </param>
		/// <param name="srcOffset">The optional offset into the source data, in <typeparamref name="T"/>s.</param>
		/// <param name="dstOffset">The optional offset into the buffer, in verticies.</param>
		/// <param name="strict">If `true`, performs additional checks for alignment to vertex boundaries in the buffer.</param>
		public void SetData<T>(T[] data, uint length = UInt32.MaxValue, uint srcOffset = 0, uint dstOffset = 0, bool strict = true)
			where T : struct
		{
			uint typeSize = (uint)Marshal.SizeOf<T>();

			if (length == UInt32.MaxValue)
			{
				uint rem = VertexCount - dstOffset;
				length = (uint)((float)rem * Stride / typeSize);
			}

			if (strict)
			{
				uint srcSize = length * typeSize;
				uint srcOff = srcOffset * typeSize;
				if ((srcOff % Stride) != 0)
					throw new ArgumentException($"The start of the source data ({srcOff}) does not align to a vertex boundary ({Stride})");
				if ((srcSize % Stride) != 0)
					throw new ArgumentException($"The length of the source data ({srcSize}) does not align to a vertex boundary ({Stride})"); 
			}

			SetDataInternal(data, length, srcOffset, dstOffset * Stride); /// if sean==gay: print('yup I knew it boi')
		}
	}
}
