using System;
using System.Collections.Generic;
using System.IO;

namespace Spectrum.Content
{
	/// <summary>
	/// Manages the loading and lifetime of content items from the disk. Content items managed by an instance of this
	/// type do not need to be disposed manually (although they can be), as the manager instance will clean them up
	/// when it is disposed. 
	/// <para>
	/// Multiple content managers can work with the same content pack files, sharing access to the files while 
	/// maintaining their own runtime copies of the loaded data. Additionally, manager instances can cache loaded 
	/// items to make multiple requests for the same item faster.
	/// </para>
	/// </summary>
	public sealed class ContentManager : IDisposable
	{
		private static readonly List<WeakReference<ContentManager>> s_managers =
			new List<WeakReference<ContentManager>>();
		private static readonly object s_managerLock = new object();

		#region Fields
		// The pack that this manager uses
		private readonly ContentPack _pack;

		// A list of the content items that this manager should dispose on cleanup
		// Note: This can result in a memory leak for certain poorly-desgined objects that are collected without being
		//       disposed (use finalizers people!), because they are stored as weak references.
		private readonly List<WeakReference<IDisposableContent>> _disposableItems;

		// Cached items
		private readonly Dictionary<string, object> _itemCache;

		private bool _isDisposed = false;
		#endregion // Fields

		private ContentManager(ContentPack pack)
		{
			_pack = pack;
			_disposableItems = new List<WeakReference<IDisposableContent>>();
			_itemCache = new Dictionary<string, object>();

			AddManager(this);
		}
		~ContentManager()
		{
			dispose(false);
		}

		/// <summary>
		/// Disposes and removes all items that are being tracked by the manager. Note that this will invalidate
		/// all items that implement <see cref="IDisposableContent"/> that were loaded through this manager. This
		/// function also clears the cached items.
		/// </summary>
		public void Unload()
		{
			foreach (var cref in _disposableItems)
			{
				if (cref.TryGetTarget(out var target) && !target.IsDisposed)
					target.Dispose();
			}
			_disposableItems.Clear();
			_itemCache.Clear();
		}

		/// <summary>
		/// Creates a new content manager for loading content from the given content pack file.
		/// </summary>
		/// <param name="path">A path to a `.cpak` file to load, or a directory to search for a `Content.cpak` file.</param>
		/// <returns>The new content manager.</returns>
		public static ContentManager OpenPackFile(string path)
		{
			// Convert to a valid cpak path
			if (!path.EndsWith(ContentPack.FILE_EXTENSION))
				path = Path.Combine(path, $"Content{ContentPack.FILE_EXTENSION}");
			path = Path.GetFullPath(path);

			// Try to get/load the content pack
			var pack = ContentPack.GetOrLoad(path);
			return new ContentManager(pack);
		}

		private static void AddManager(ContentManager cm)
		{
			lock (s_managerLock)
			{
				// Add the manager, and also remove any disposed managers from the list
				s_managers.Add(new WeakReference<ContentManager>(cm));
				for (int i = s_managers.Count - 2; i >= 0; --i)
				{
					if (!s_managers[i].TryGetTarget(out var target))
						s_managers.RemoveAt(i);
				}
			}
		}

		private static void RemoveManager(ContentManager cm)
		{
			lock (s_managerLock)
			{
				// Remove the manager, and also remove any disposed managers from the list
				for (int i = s_managers.Count - 1; i >= 0; --i)
				{
					if (!s_managers[i].TryGetTarget(out var target) || ReferenceEquals(cm, target))
						s_managers.RemoveAt(i);
				}
			}
		}

		#region IDiposable
		public void Dispose()
		{
			dispose(true);
			GC.SuppressFinalize(this);
		}

		private void dispose(bool disposing)
		{
			if (!_isDisposed)
			{
				if (disposing)
					Unload(); // Since the important items implement IDisposable, they will take care of themselves
				RemoveManager(this);
			}
			_isDisposed = true;
		}
		#endregion // IDisposable
	}
}
