﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace Spectrum.Utilities
{
	/// <summary>
	/// Contains extra utility functions for working with collections and arrays.
	/// </summary>
	public static class CollectionUtils
	{
		/// <summary>
		/// Implements a foreach loop as a Linq-style function call.
		/// </summary>
		/// <typeparam name="T">The type of the array.</typeparam>
		/// <param name="arr">The array to iterate over.</param>
		/// <param name="func">The function to run on each member in the array.</param>
		public static void ForEach<T>(this T[] arr, Action<T> func)
		{
			if (arr == null)
				throw new ArgumentNullException(nameof(arr));

			foreach (var val in arr)
				func(val);
		}

		/// <summary>
		/// Implements a foreach loop as a Linq-stle function call.
		/// </summary>
		/// <typeparam name="T">The type held by the enumerator.</typeparam>
		/// <param name="enumer">The enumerable collection to iterate over.</param>
		/// <param name="func">The function to run on each member of the enumerator.</param>
		public static void ForEach<T>(this IEnumerable<T> enumer, Action<T> func)
		{
			if (enumer == null)
				throw new ArgumentNullException(nameof(enumer));

			foreach (var val in enumer)
				func(val);
		}

		/// <summary>
		/// Orders the input values based on a key by ascending order, but returns the sorted indices of the original
		/// collection, instead of the sorted array.
		/// </summary>
		/// <typeparam name="T">The type of the input values.</typeparam>
		/// <typeparam name="TKey">The key type used to order the values.</typeparam>
		/// <param name="enumer">The input values to get the sorted indices for.</param>
		/// <param name="keySelector">The function to get a key for each input value.</param>
		/// <returns>The sorted indices of the input array, such that `Input[Return[i]] = Sorted[i]`.</returns>
		public static int[] ArgSort<T, TKey>(this IEnumerable<T> enumer, Func<T, TKey> keySelector)
		{
			var keyPairs = enumer
				.Select((val, idx) => (key: keySelector(val), idx: idx))
				.OrderBy(pair => pair.key)
				.Select(pair => pair.idx);
			return keyPairs.ToArray();
		}

		/// <summary>
		/// Orders the input values based on a key, using the provided comparer, but returns the sorted indices of the
		/// original collection, instead of the sorted array.
		/// </summary>
		/// <typeparam name="T">The type of the input values.</typeparam>
		/// <typeparam name="TKey">The key type used to order the values.</typeparam>
		/// <param name="enumer">The input values to get the sorted indices for.</param>
		/// <param name="keySelector">The function to get a key for each input value.</param>
		/// <param name="comparer">The function to compare and sort the keys.</param>
		/// <returns>The sorted indices of the input array, such that `Input[Return[i]] = Sorted[i]`.</returns>
		public static int[] ArgSort<T, TKey>(this IEnumerable<T> enumer, Func<T, TKey> keySelector, IComparer<TKey> comparer)
		{
			var keyPairs = enumer
				.Select((val, idx) => (key: keySelector(val), idx: idx))
				.OrderBy(pair => pair.key, comparer)
				.Select(pair => pair.idx);
			return keyPairs.ToArray();
		}

		/// <summary>
		/// Orders the input values based on a key by ascending order, but returns the sorted indices of the original
		/// collection, instead of the sorted array.
		/// </summary>
		/// <typeparam name="T">The type of the input values.</typeparam>
		/// <typeparam name="TKey">The key type used to order the values.</typeparam>
		/// <param name="enumer">The input values to get the sorted indices for, which also provides the input value index.</param>
		/// <param name="keySelector">The function to get a key for each input value.</param>
		/// <returns>The sorted indices of the input array, such that `Input[Return[i]] = Sorted[i]`.</returns>
		public static int[] ArgSort<T, TKey>(this IEnumerable<T> enumer, Func<T, int, TKey> keySelector)
		{
			var keyPairs = enumer
				.Select((val, idx) => (key: keySelector(val, idx), idx: idx))
				.OrderBy(pair => pair.key)
				.Select(pair => pair.idx);
			return keyPairs.ToArray();
		}

		/// <summary>
		/// Orders the input values based on a key, using the provided comparer, but returns the sorted indices of the
		/// original collection, instead of the sorted array.
		/// </summary>
		/// <typeparam name="T">The type of the input values.</typeparam>
		/// <typeparam name="TKey">The key type used to order the values.</typeparam>
		/// <param name="enumer">The input values to get the sorted indices for, which also provides the input value index.</param>
		/// <param name="keySelector">The function to get a key for each input value.</param>
		/// <param name="comparer">The function to compare and sort the keys.</param>
		/// <returns>The sorted indices of the input array, such that `Input[Return[i]] = Sorted[i]`.</returns>
		public static int[] ArgSort<T, TKey>(this IEnumerable<T> enumer, Func<T, int, TKey> keySelector, IComparer<TKey> comparer)
		{
			var keyPairs = enumer
				.Select((val, idx) => (key: keySelector(val, idx), idx: idx))
				.OrderBy(pair => pair.key, comparer)
				.Select(pair => pair.idx);
			return keyPairs.ToArray();
		}
	}
}