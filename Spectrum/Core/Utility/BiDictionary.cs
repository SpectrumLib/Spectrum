/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2018-2019 The Spectrum Team
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */
using System;
using System.Collections;
using System.Collections.Generic;

namespace Spectrum
{
	/// <summary>
	/// A dictionary type that contains a bi-directional one-to-one mapping between objects.
	/// </summary>
	public sealed class BiDictionary<T1, T2> : IEnumerable<(T1, T2)>
	{
		private const int DEFAULT_MAP_SIZE = 16;

		#region Fields
		private readonly Dictionary<T1, T2> _1_2_map;
		private readonly Dictionary<T2, T1> _2_1_map;
		#endregion // Fields

		/// <summary>
		/// Creates a new empty BiDictionary.
		/// </summary>
		public BiDictionary() :
			this(DEFAULT_MAP_SIZE)
		{ }
		/// <summary>
		/// Creates a new empty BiDictionary with the given initial capacity.
		/// </summary>
		/// <param name="cap">The initial capacity.</param>
		public BiDictionary(uint cap)
		{
			_1_2_map = new Dictionary<T1, T2>((int)cap);
			_2_1_map = new Dictionary<T2, T1>((int)cap);
		}

		#region Add/Set
		/// <summary>
		/// Attempts to add a new object mapping to the dictionary. If either item already exists in a mapping, an
		/// exception is thrown.
		/// </summary>
		/// <param name="item1">The first item in the pair.</param>
		/// <param name="item2">The second item in the pair.</param>
		/// <exception cref="ArgumentException">One of the values in the pair already exists in the dictionary.</exception>
		public void Add(T1 item1, T2 item2)
		{
			if (_1_2_map.ContainsKey(item1))
				throw new ArgumentException($"Duplicate first item {item1}", nameof(item1));
			if (_2_1_map.ContainsKey(item2))
				throw new ArgumentException($"Duplicate second item {item2}", nameof(item2));
			_1_2_map.Add(item1, item2);
			_2_1_map.Add(item2, item1);
		}

		/// <summary>
		/// Sets the mapping in the dictionary. If either value already existed in a mapping, the mapping is updated.
		/// </summary>
		/// <param name="item1">The first item in the pair.</param>
		/// <param name="item2">The second item in the pair.</param>
		/// <returns>A boolean pair giving if the associated item had an existing mapping that was updated.</returns>
		public (bool, bool) Set(T1 item1, T2 item2)
		{
			var ret = (
				_1_2_map.TryGetValue(item1, out var o2),
				_2_1_map.TryGetValue(item2, out var o1)
			);
			if (ret.Item1)
				_2_1_map.Remove(o2);
			if (ret.Item2)
				_1_2_map.Remove(o1);

			_1_2_map[item1] = item2;
			_2_1_map[item2] = item1;
			return ret;
		}

		/// <summary>
		/// Sets the mapping in the dictionary. If either value already existed in a mapping, the mapping is updated.
		/// </summary>
		/// <param name="item1">The first item in the pair.</param>
		/// <param name="item2">The second item in the pair.</param>
		/// <param name="old1">The old value of the first item mapped to <paramref name="item2"/>.</param>
		/// <param name="old2">The old value of the second item mapped to <paramref name="item1"/>.</param>
		/// <returns>A boolean pair giving if the associated item had an existing mapping that was updated.</returns>
		public (bool, bool) Set(T1 item1, T2 item2, out T1 old1, out T2 old2)
		{
			var ret = (
				_1_2_map.TryGetValue(item1, out old2),
				_2_1_map.TryGetValue(item2, out old1)
			);
			if (ret.Item1)
				_2_1_map.Remove(old2);
			if (ret.Item2)
				_1_2_map.Remove(old1);

			_1_2_map[item1] = item2;
			_2_1_map[item2] = item1;
			return ret;
		}
		#endregion // Add/Set

		#region Remove
		/// <summary>
		/// Removes the mapping with the given first item.
		/// </summary>
		/// <param name="key">The first item of the mapping to remove.</param>
		/// <returns>If a mapping was found and removed.</returns>
		public bool RemoveByFirst(T1 key)
		{
			if (_1_2_map.Remove(key, out var t2))
			{
				_2_1_map.Remove(t2);
				return true;
			}
			return false;
		}

		/// <summary>
		/// Removes the mapping with the given first item.
		/// </summary>
		/// <param name="key">The first item of the mapping to remove.</param>
		/// <param name="value">The second item of the mapping that was removed.</param>
		/// <returns>If a mapping was found and removed.</returns>
		public bool RemoveByFirst(T1 key, out T2 value)
		{
			if (_1_2_map.Remove(key, out value))
			{
				_2_1_map.Remove(value);
				return true;
			}
			return false;
		}

		/// <summary>
		/// Removes the mapping with the given second item.
		/// </summary>
		/// <param name="key">The second item of the mapping to remove.</param>
		/// <returns>If a mapping was found and removed.</returns>
		public bool RemoveBySecond(T2 key)
		{
			if (_2_1_map.Remove(key, out var t1))
			{
				_1_2_map.Remove(t1);
				return true;
			}
			return false;
		}

		/// <summary>
		/// Removes the mapping with the given second item.
		/// </summary>
		/// <param name="key">The second item of the mapping to remove.</param>
		/// <param name="value">The first item of the mapping that was removed.</param>
		/// <returns>If a mapping was found and removed.</returns>
		public bool RemoveBySecond(T2 key, out T1 value)
		{
			if (_2_1_map.Remove(key, out value))
			{
				_1_2_map.Remove(value);
				return true;
			}
			return false;
		}
		#endregion // Remove

		#region Get
		/// <summary>
		/// Gets the second item value mapped with the given first item value.
		/// </summary>
		/// <param name="key">The mapping first item to get.</param>
		/// <returns>The second item paired with the first item key.</returns>
		/// <exception cref="ArgumentException">There is no mapping with the given first item.</exception>
		public T2 GetByFirst(T1 key)
		{
			if (_1_2_map.TryGetValue(key, out var t2))
				return t2;
			else
				throw new ArgumentException($"No first item with value {key}", nameof(key));
		}

		/// <summary>
		/// Gets the first item value mapped with the given second item value.
		/// </summary>
		/// <param name="key">The mapping second item to get.</param>
		/// <returns>The first item paired with the second item key.</returns>
		/// <exception cref="ArgumentException">There is no mapping with the given second item.</exception>
		public T1 GetBySecond(T2 key)
		{
			if (_2_1_map.TryGetValue(key, out var t1))
				return t1;
			else
				throw new ArgumentException($"No second item with value {key}", nameof(key));
		}

		/// <summary>
		/// Tries to get the second item value mapped with the first item value.
		/// </summary>
		/// <param name="key">The first item value to search for.</param>
		/// <param name="value">The found second item value.</param>
		/// <returns>If the mapping with the first item could be found.</returns>
		public bool TryGetByFirst(T1 key, out T2 value) => _1_2_map.TryGetValue(key, out value);

		/// <summary>
		/// Tries to get the first item value mapped with the second item value.
		/// </summary>
		/// <param name="key">The second item value to search for.</param>
		/// <param name="value">The found first item value.</param>
		/// <returns>If the mapping with the second item could be found.</returns>
		public bool TryGetBySecond(T2 key, out T1 value) => _2_1_map.TryGetValue(key, out value);
		#endregion // Get

		#region Contains
		/// <summary>
		/// Gets if there is a mapping with the given first item value.
		/// </summary>
		/// <param name="item1">The first item value to check for.</param>
		/// <returns>If there is a mapping.</returns>
		public bool ContainsFirst(T1 item1) => _1_2_map.TryGetValue(item1, out _);

		/// <summary>
		/// Gets if there is a mapping with the given second item value.
		/// </summary>
		/// <param name="item1">The second item value to check for.</param>
		/// <returns>If there is a mapping.</returns>
		public bool ContainsSecond(T2 item2) => _2_1_map.TryGetValue(item2, out _);
		#endregion // Contains

		public IEnumerator<(T1, T2)> GetEnumerator()
		{
			foreach (var pair in _1_2_map)
				yield return (pair.Key, pair.Value);
		}

		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
	}
}
