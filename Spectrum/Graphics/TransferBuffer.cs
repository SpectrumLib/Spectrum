/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2018-2019 The Spectrum Team
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */
using System;
using Vk = SharpVk;

namespace Spectrum.Graphics
{
	// Used to transfer data to/from the graphics device
	internal sealed class TransferBuffer : IDisposable
	{
		public const uint SIZE = 8 * 1024 * 1024; // 8MB transfer buffer

		#region Fields
		public readonly uint Index;

		// Buffer/memory objects
		private readonly Vk.Buffer _buffer;
		private readonly Vk.DeviceMemory _memory;
		private readonly uint _offset;
		private readonly bool _coherent;

		// Commanding objects
		private readonly Vk.CommandBuffer _commands;
		private readonly Vk.Fence _fence;
		#endregion // Fields

		public TransferBuffer(uint index, ThreadGraphicsObjects tgo)
		{
			Index = index;

			_buffer = tgo.TransferPool[index].Buffer;
			_memory = tgo.TransferMemory;
			_offset = index * SIZE;
			_coherent = tgo.CoherentTransfer;

			_commands = tgo.TransferPool[index].Commands;
			_fence = tgo.TransferPool[index].Fence;
		}

		public void Wait() => _fence.Wait(UInt64.MaxValue);

		#region IDisposable
		public void Dispose()
		{
			Core.Instance.GraphicsDevice.ReleaseTransferBuffer(this);
		}
		#endregion // IDisposable
	}
}
