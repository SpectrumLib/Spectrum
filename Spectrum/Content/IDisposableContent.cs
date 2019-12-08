/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2018-2019 The Spectrum Team
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */
using System;

namespace Spectrum.Content
{
	/// <summary>
	/// Uses events to signal <see cref="ContentManager"/> instances when objects that they are monitoring are
	/// disposed, to avoid disposing an object twice. All content types that can be loaded from a
	/// <see cref="ContentManager"/>, that also are disposable, should implement this interface, and call the
	/// </summary>
	public interface IDisposableContent : IDisposable
	{
		/// <summary>
		/// The event that must be triggered when the object is disposed.
		/// </summary>
		event EventHandler ObjectDisposed;
	}
}
