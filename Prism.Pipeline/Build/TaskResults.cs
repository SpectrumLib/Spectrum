using System;
using System.Collections.Generic;
using Prism.Content;

namespace Prism.Build
{
	// Holds the results of a build task
	internal class TaskResults
	{
		#region Fields
		// Includes successful and skipped items, with their file sizes
		// RealSize is the size of the saved binary data, and UCSize is the size of the data when uncompressed
		private readonly List<(ContentItem, uint, uint, bool)> _passItems;
		public IReadOnlyList<(ContentItem Item, uint RealSize, uint UCSize, bool Skipped)> PassItems => _passItems;

		// Failed items
		private readonly List<ContentItem> _failItems;
		public IReadOnlyList<ContentItem> FailItems => _failItems;

		// Item counts
		public uint PassCount => (uint)_passItems.Count;
		public uint SkipCount { get; private set; } = 0;
		public uint FailCount => (uint)_failItems.Count;

		// Tracks the items being worked on
		private BuildEvent _currentEvent = null;
		#endregion // Fields

		public TaskResults()
		{
			_passItems = new List<(ContentItem, uint, uint, bool)>();
			_failItems = new List<ContentItem>();
		}

		public void Reset()
		{
			_passItems.Clear();
			_failItems.Clear();
			_currentEvent = null;
			SkipCount = 0;
		}

		public void UseItem(BuildEvent evt)
		{
			if (_currentEvent != null)
				_failItems.Add(_currentEvent.Item);
			_currentEvent = evt;
		}

		public void PassItem(uint ucsize, bool skipped)
		{
			_passItems.Add((_currentEvent.Item, ucsize, ucsize, skipped));
			_currentEvent = null;
			if (skipped)
				SkipCount += 1;
		}

		// Will update the previous item with the real size
		public void UpdatePreviousItem(uint realsize)
		{
			var copy = _passItems[_passItems.Count - 1];
			copy.Item2 = realsize;
			_passItems[_passItems.Count - 1] = copy;
		}
	}
}
