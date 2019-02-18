using System;

namespace Spectrum.Content
{
	/// <summary>
	/// Base interface for any type that can be loaded by a <see cref="ContentManager"/> instance, but should also be
	/// disposable by implementing the <see cref="IDisposable"/> interface. Content types that do not implement this
	/// type cannot be tracked and managed by ContentManager instances.
	/// <para>
	/// Types that implement this interface should be written so that they are properly disposed through their
	/// finalizers as well. Types that do not do this have a chance of leaking unmanaged resources, as the
	/// ContentManager that loaded the instance does not hold a strong reference to the item.
	/// </para>
	/// </summary>
	public interface IDisposableContent : IDisposable
	{
		/// <summary>
		/// Gets if the item is disposed. This is used to tell the <see cref="ContentManager"/> instance managing
		/// this object (if there is one) whether or not to dispose this object on cleanup.
		/// </summary>
		bool IsDisposed { get; }
	}
}
