using System;
using System.Collections.Generic;

namespace Prism.Build
{
	// Holds the results of a build task
	internal class TaskResults
	{
		#region Fields
		// Includes successful and skipped items, with their file sizes
		private readonly Dictionary<string, uint> _passItems;
		public IReadOnlyDictionary<string, uint> PassItems => _passItems;

		// Failed items
		private readonly List<string> _failItems;
		public IReadOnlyList<string> FailItems => _failItems;

		// Item counts
		public uint PassCount => (uint)_passItems.Count;
		public uint FailCount => (uint)_failItems.Count;

		// Tracks the current item being worked on
		private string _currentItem = null;
		#endregion // Fields

		public TaskResults()
		{
			_passItems = new Dictionary<string, uint>();
			_failItems = new List<string>();
		}

		public void Reset()
		{
			_passItems.Clear();
			_failItems.Clear();
			_currentItem = null;
		}

		public void UseItem(string name)
		{
			if (_currentItem != null)
				_failItems.Add(_currentItem);
			_currentItem = name;
		}

		public void PassItem(uint size)
		{
			_passItems.Add(_currentItem, size);
			_currentItem = null;
		}
	}
}
