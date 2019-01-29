using System;
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
		/// Implements a foreach loop as a Linq-stle function call, providing the function with the collection index.
		/// </summary>
		/// <typeparam name="T">The type held by the enumerator.</typeparam>
		/// <param name="enumer">The enumerable collection to iterate over.</param>
		/// <param name="func">The function to run on each member of the enumerator.</param>
		public static void ForEach<T>(this IEnumerable<T> enumer, Action<T, int> func)
		{
			if (enumer == null)
				throw new ArgumentNullException(nameof(enumer));

			int index = 0;
			foreach (var val in enumer)
				func(val, index++);
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

		/// <summary>
		/// Finds the index of the first value in the collection that matches the predicate.
		/// </summary>
		/// <typeparam name="T">The type stored in the collection.</typeparam>
		/// <param name="coll">The collection to search in.</param>
		/// <param name="predicate">The predicate to search with.</param>
		/// <returns>The index of the matching value, or -1 if no indices matched.</returns>
		public static int IndexOf<T>(this IReadOnlyCollection<T> coll, Func<T, bool> predicate)
		{
			int idx = 0;
			foreach (var item in coll)
			{
				if (predicate(item))
					return idx;
				++idx;
			}
			return -1;
		}
	}
}
