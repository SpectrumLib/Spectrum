/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2018-2020 The Spectrum Team
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */
using System;

namespace Prism.Pipeline
{
	// Contains information about the build results of a content item
	internal sealed class ItemResult
	{
		#region Fields
		public readonly ContentItem Item;
		public readonly uint Index;

		public bool Success { get; private set; }
		public bool Skipped { get; private set; }
		public TimeSpan BuildTime { get; private set; }
		public ulong Size { get; private set; } // Final size of the generated binary data
		public bool Compress { get; private set; } // If the item data should be compressed
		#endregion // Fields

		public ItemResult(BuildOrder order)
		{
			Item = order.Item;
			Index = order.Index;

			Success = false;
			Skipped = false;
			BuildTime = TimeSpan.Zero;
			Size = 0;
			Compress = false;
		}

		// Marks the result as a success
		public void Complete(TimeSpan time, ulong size, bool compress)
		{
			Success = true;
			Skipped = false;
			BuildTime = time;
			Size = size;
			Compress = compress;
		}

		public void Skip(TimeSpan time, ulong size, bool compress)
		{
			Complete(time, size, compress);
			Skipped = true;
		}
	}
}
