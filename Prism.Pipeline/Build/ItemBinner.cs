using System;
using System.Collections.Generic;
using System.Linq;
using Prism.Content;

namespace Prism.Build
{
	// Contains the logic for sorting items, and binning them into packs
	internal class ItemBinner
	{
		#region Fields
		public readonly PackingProcess Process;
		public BuildEngine Engine => Process.Engine;
		public ContentProject Project => Process.Engine.Project;

		private readonly IEnumerable<(ContentItem Item, uint Size, bool Skipped)> _items;

		private readonly List<ItemBin> _bins;
		public IReadOnlyList<ItemBin> Bins => _bins;
		#endregion // Fields

		public ItemBinner(PackingProcess proc, BuildTask[] tasks)
		{
			Process = proc;

			_items = tasks
				.Select(task => task.Results)
				.SelectMany(result => result.PassItems);
			_bins = new List<ItemBin>();
		}

		public bool MakeBins()
		{
			// Sort the items by size (largest first)
			var sorted = _items
				.OrderByDescending(item => item.Size)
				.ToArray();

			// Check that they will all fit
			uint limit = Project.Properties.PackSize;
			var tooBig = sorted.Where(item => item.Size > limit);
			if (tooBig.Any())
			{
				int count = tooBig.Count();
				int max = Math.Min(5, count);
				Engine.Logger.EngineError("There are one or more content items that are too large for the current pack size:");
				foreach (var item in tooBig.Take(max))
					Engine.Logger.EngineError($"    - {item.Item.ItemPath}");
				if (count > 5)
					Engine.Logger.EngineError($"    - and {count - 5} more...");
				return false;
			}

			// Continue looping until every single item is assigned to a bin
			uint remaining = (uint)sorted.Length;
			uint binNum = 0;
			var seen = Enumerable.Repeat(false, sorted.Length).ToArray();
			while (remaining > 0)
			{
				// Create a new bin
				ItemBin cbin = new ItemBin(this, binNum++);

				// Loop through the items, trying to add them if they are not yet assigned
				for (int idx = 0; idx < sorted.Length; ++idx)
				{
					if (seen[idx])
						continue;

					if (cbin.TryAdd(sorted[idx]))
					{
						seen[idx] = true;
						remaining -= 1;
					}
				}

				// Add the bin and continue
				_bins.Add(cbin);
			}

			// All done
			return true;
		}
	}

	// Holds items that all go into a single pack file
	internal class ItemBin
	{
		#region Fields
		public readonly ItemBinner Binner;
		public readonly uint BinNumber;
		public uint SizeLimit => Binner.Project.Properties.PackSize;

		// Offsets are from the start of the item data, not the start of the file
		private readonly List<(ContentItem Item, uint Size, uint Offset)> _items;
		public IReadOnlyList<(ContentItem Item, uint Size, uint Offset)> Items => _items;

		public uint TotalSize { get; private set; }
		#endregion // Fields

		public ItemBin(ItemBinner binner, uint number)
		{
			Binner = binner;
			BinNumber = number;
			_items = new List<(ContentItem Item, uint Size, uint Offset)>();
			TotalSize = 0;
		}

		public bool TryAdd(in (ContentItem Item, uint Size, bool Skipped) item)
		{
			if ((TotalSize + item.Size) > SizeLimit)
				return false;

			_items.Add((item.Item, item.Size, TotalSize));
			TotalSize += item.Size;
			return true;
		}
	}
}
