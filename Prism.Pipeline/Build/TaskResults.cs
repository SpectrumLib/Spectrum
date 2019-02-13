using System;
using System.Collections.Generic;

namespace Prism.Build
{
	// Holds the results of a build task
	internal class TaskResults
	{
		#region Fields
		// Includes successful and skipped items, with their file sizes
		private readonly Dictionary<string, (uint, bool)> _passItems;
		public IReadOnlyDictionary<string, (uint Size, bool Skipped)> PassItems => _passItems;

		// Failed items
		private readonly List<string> _failItems;
		public IReadOnlyList<string> FailItems => _failItems;

		// Item counts
		public uint PassCount => (uint)_passItems.Count;
		public uint SkipCount { get; private set; } = 0;
		public uint FailCount => (uint)_failItems.Count;

		// Tracks the current item being worked on
		private string _currentItem = null;
		#endregion // Fields

		public TaskResults()
		{
			_passItems = new Dictionary<string, (uint, bool)>();
			_failItems = new List<string>();
		}

		public void Reset()
		{
			_passItems.Clear();
			_failItems.Clear();
			_currentItem = null;
			SkipCount = 0;
		}

		public void UseItem(string name)
		{
			if (_currentItem != null)
				_failItems.Add(_currentItem);
			_currentItem = name;
		}

		public void PassItem(uint size, bool skipped)
		{
			_passItems.Add(_currentItem, (size, skipped));
			_currentItem = null;
			if (skipped)
				SkipCount += 1;
		}
	}
}
