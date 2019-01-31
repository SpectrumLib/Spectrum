using System;
using System.Collections.Generic;

namespace Spectrum.Utilities
{
	/// <summary>
	/// A simple type for testing for equality of objects using a direct reference comparison with
	/// <see cref="Object.ReferenceEquals(object, object)"/>. This can be used in any of the System.Linq functions that
	/// require an <see cref="IEqualityComparer{T}"/>, such as Distinct(), Except(), Intersect(), and Union().
	/// </summary>
	/// <typeparam name="T">The type to compare.</typeparam>
	public struct RefComparer<T> : IEqualityComparer<T>
	{
		bool IEqualityComparer<T>.Equals(T x, T y) => Object.ReferenceEquals(x, y);

		int IEqualityComparer<T>.GetHashCode(T obj) => obj.GetHashCode();
	}
}
