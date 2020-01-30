/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2018-2020 The Spectrum Team
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */
using System;
using System.IO;
using System.Runtime.InteropServices;

namespace Prism.Pipeline
{
	// Passthrough processor, which just copies content data through un-altered.
	//   This is a special type, which acts as the default processor for all unknown content file types.
	//   It can also be selected with the "None" content type
	[ContentProcessor("PassthroughProcessor", "None", "")]
	internal sealed class PassthroughProcessor : ContentProcessor
	{
		// This type only copies through 512 KB at a time
		// This makes the type more responsive to build process cancel requests
		// It also allows us to examine/modify the data at a later date if we want to add more functionality

		private const int HEAP_BUFFER_SIZE = 512 * 1024; // 512 KB

		#region Fields
		private int _lastReadCount;

		private readonly IntPtr _heapBuffer;
		#endregion // Fields

		public PassthroughProcessor()
		{
			_heapBuffer = Marshal.AllocHGlobal(HEAP_BUFFER_SIZE);
		}

		public override void Reset()
		{
			_lastReadCount = 0;
		}

		public override void Begin(PipelineContext ctx, BinaryReader stream)
		{
			
		}

		public unsafe override bool Read(PipelineContext ctx, BinaryReader stream)
		{
			_lastReadCount = stream.Read(new Span<byte>(_heapBuffer.ToPointer(), HEAP_BUFFER_SIZE));
			return (_lastReadCount != 0);
		}

		public override void Process(PipelineContext ctx)
		{
			// NOP - passthrough
		}

		public unsafe override void Write(PipelineContext ctx, BinaryWriter stream)
		{
			stream.Write(new ReadOnlySpan<byte>(_heapBuffer.ToPointer(), _lastReadCount));
		}

		public override void End(PipelineContext ctx, BinaryWriter stream, out bool compress)
		{
			compress = true;
		}

		protected override void onDispose(bool disposing)
		{
			Marshal.FreeHGlobal(_heapBuffer);
		}
	}
}
