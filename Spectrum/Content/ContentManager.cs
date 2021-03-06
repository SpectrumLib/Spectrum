﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;

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

		/// <summary>
		/// A mapping of the names of content items to their cached objects, if one exists.
		/// </summary>
		public IReadOnlyDictionary<string, object> Cache => _itemCache;

		// Filestreams to the bin files (index by bin index), null for debug builds
		private readonly List<FileStream> _binStreams;
		// List of loader instances in this content manager
		private readonly Dictionary<uint, IContentLoader> _loaders;

		/// <summary>
		/// The absolute path to the .cpak file this content manager was opened with.
		/// </summary>
		public string FilePath => _pack.FilePath;
		/// <summary>
		/// Gets if the content managed by the instance was build in release mode.
		/// </summary>
		public bool IsRelease => _pack.ReleaseMode;

		/// <summary>
		/// Gets the amount of time that it took to load the last content item using one of the <see cref="LoadRaw(string)"/>,
		/// <see cref="Load{T}(string, bool, bool)"/>, <see cref="LoadLocalized{T}(string, CultureInfo, bool, bool, bool)"/>,
		/// or <see cref="Reload{T}(string, bool, bool)"/> functions.
		/// </summary>
		public TimeSpan LastLoadTime { get; private set; } = TimeSpan.Zero;

		private bool _isDisposed = false;
		#endregion // Fields

		private ContentManager(ContentPack pack)
		{
			_pack = pack;
			_disposableItems = new List<WeakReference<IDisposableContent>>();
			_itemCache = new Dictionary<string, object>();

			_loaders = new Dictionary<uint, IContentLoader>(pack.Loaders.Count);

			if (pack.ReleaseMode)
			{
				_binStreams = pack.BinFiles
					.Select(bf => bf.OpenStream())
					.ToList();
			}
			else
				_binStreams = null;

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
		/// Gets if the manager instance is currently caching the content item with the name and matching type.
		/// </summary>
		/// <typeparam name="T">The type of the content item to check is cached.</typeparam>
		/// <param name="name">The content item name to check for.</param>
		/// <returns>If the content item with the name is cached, and the cached item is the same type.</returns>
		public bool IsItemCached<T>(string name) where T : class => (_itemCache.TryGetValue(name, out var obj) && (obj is T));

		/// <summary>
		/// Sets the manager to stop tracking the item's lifetime. After this call, the item must be manually managed
		/// and disposed.
		/// </summary>
		/// <param name="item">The item for this manager to stop tracking.</param>
		/// <returns>If the item was previously tracked by this manager, and is no longer being tracked.</returns>
		public bool StopTracking(IDisposableContent item)
		{
			if (item == null)
				throw new ArgumentNullException(nameof(item));

			// Remove the item from tracking, and also any items that may have been collected
			bool found = false;
			for (int i = _disposableItems.Count - 1; i >= 0; --i)
			{
				if (!_disposableItems[i].TryGetTarget(out var target))
					_disposableItems.RemoveAt(i);
				if (ReferenceEquals(target, item))
				{
					_disposableItems.RemoveAt(i);
					found = true;
				}
			}

			return found;
		}

		#region Load Functions
		/// <summary>
		/// Attempts to load the given content item as the provided type. Will first check the cached items, and then
		/// will load the content item from the disk. If the item should be loaded regardless of whether or not it
		/// is in the cache, then you should use <see cref="Reload{T}(string, bool, bool)"/>.
		/// </summary>
		/// <typeparam name="T">The type to load the content data into.</typeparam>
		/// <param name="name">The name of the content item to load, without the extension.</param>
		/// <param name="cache">If the manager should cache the result of this load operation for later use.</param>
		/// <param name="manage">
		/// If the manager should manage the lifetime of the result of this load operation. This is only valid if the
		/// type <typeparamref name="T"/> implements <see cref="IDisposableContent"/>, and is ignored otherwise.
		/// </param>
		/// <returns>The cached or newly loaded content item.</returns>
		public T Load<T>(string name, bool cache = true, bool manage = true)
			where T : class
		{
			if (String.IsNullOrWhiteSpace(name))
				throw new ArgumentException("The content item name cannot be null or whitespace.", nameof(name));

			// Check the cache first
			if (_itemCache.TryGetValue(name, out var item) && (item is T))
			{
				LastLoadTime = TimeSpan.Zero;
				return item as T;
			}

			Stopwatch timer = Stopwatch.StartNew();
			// Load in the new content item
			item = readContentItem(name, typeof(T));
			if (cache)
				_itemCache.Add(name, item);
			if (manage && (item is IDisposableContent))
				_disposableItems.Add(new WeakReference<IDisposableContent>((IDisposableContent)item));
			LastLoadTime = timer.Elapsed;
			return item as T;
		}

		/// <summary>
		/// Attempts to load a localized content item from the disk. Localized content items are specified by appending
		/// the language code, or language and region code, to the end of the file name. 
		/// <para>
		/// This function will auto-detect the correct file to look for (without the user having to specify the region) 
		/// based off of the culture info passed to the function. It will first look for region-specific items 
		/// (e.g. `en-US`), then language-specific items (e.g. `en`), then will fall back on the unlocalized version of 
		/// the item.
		/// </para>
		/// </summary>
		/// <typeparam name="T">The type to load the content data into.</typeparam>
		/// <param name="name">The name of the content item to load, without the extension.</param>
		/// <param name="culture">The culture to use as the localization, or <c>null</c> to use the current culture.</param>
		/// <param name="cache">If the manager should cache the result of this load operation for later use.</param>
		/// <param name="manage">
		/// If the manager should manage the lifetime of the result of this load operation. This is only valid if the
		/// type <typeparamref name="T"/> implements <see cref="IDisposableContent"/>, and is ignored otherwise.
		/// </param>
		/// <param name="reload">
		/// If the <see cref="Reload{T}(string, bool, bool)"/> function should be used to load the localized content
		/// item, instead of the standard <see cref="Load{T}(string, bool, bool)"/> function.
		/// </param>
		/// <returns>The cached or newly loaded content item.</returns>
		public T LoadLocalized<T>(string name, CultureInfo culture = null, bool cache = true, bool manage = true, bool reload = false)
			where T : class
		{
			if (String.IsNullOrWhiteSpace(name))
				throw new ArgumentException("The content item name cannot be null or whitespace.", nameof(name));
			if (culture == null)
				culture = CultureInfo.CurrentCulture;

			// Try by region first
			var tname = name + culture.Name; // e.g. "en-US"
			try
			{
				return reload ? Reload<T>(tname, cache, manage) : Load<T>(tname, cache, manage);
			}
			catch (ContentLoadException) { }

			// Try by langauge next
			tname = name + culture.TwoLetterISOLanguageName; // e.g. "en"
			try
			{
				return reload ? Reload<T>(tname, cache, manage) : Load<T>(tname, cache, manage);
			}
			catch (ContentLoadException) { }

			// Localized asset not found, load the non-localized one
			return reload ? Reload<T>(name, cache, manage) : Load<T>(name, cache, manage);
		}

		/// <summary>
		/// Similar to the <see cref="Load{T}(string, bool, bool)"/> function, but does not check the cache for the
		/// item. It instead attempts to load the item directly off of the disk.
		/// </summary>
		/// <typeparam name="T">The type to load the content data into.</typeparam>
		/// <param name="name">The name of the content item to load, without the extension.</param>
		/// <param name="cache">If the manager should cache the result of this load operation for later use.</param>
		/// <param name="manage">
		/// If the manager should manage the lifetime of the result of this load operation. This is only valid if the
		/// type <typeparamref name="T"/> implements <see cref="IDisposableContent"/>, and is ignored otherwise.
		/// </param>
		/// <returns>The newly loaded content item.</returns>
		public T Reload<T>(string name, bool cache = true, bool manage = true)
			where T : class
		{
			if (String.IsNullOrWhiteSpace(name))
				throw new ArgumentException("The content item name cannot be null or whitespace.", nameof(name));

			// Load in the new content item
			Stopwatch timer = Stopwatch.StartNew();
			var item = readContentItem(name, typeof(T));
			if (cache)
				_itemCache.Add(name, item);
			if (manage && (item is IDisposableContent))
				_disposableItems.Add(new WeakReference<IDisposableContent>((IDisposableContent)item));
			LastLoadTime = timer.Elapsed;
			return item as T;
		}

		/// <summary>
		/// Loads the content item data from the disk as a raw byte array, with no processing applied. This function
		/// performs no caching or lifetime management for the data.
		/// </summary>
		/// <remarks>
		/// Note that this loads the entirety of the content data at once, while may cause speed or memory problems 
		/// for very large files.
		/// </remarks>
		/// <param name="name">The name of the content item to load, without the extension.</param>
		/// <returns>The raw data of the content item.</returns>
		public byte[] LoadRaw(string name)
		{
			Stopwatch timer = Stopwatch.StartNew();
			if (_pack.ReleaseMode)
			{
				if (!_pack.TryGetItem(name, out var binNum, out var item))
					throw new ContentLoadException(name, "the item does not exist in the content pack.");

				ContentStream stream = null;
				try
				{
					var fstream = getOrOpenBinStream(binNum);
					stream = new ContentStream(name, fstream, item.Offset, item.RealSize, item.UCSize);
					var ret = stream.ReadBytes(item.UCSize);
					LastLoadTime = timer.Elapsed;
					return ret;
				}
				catch (Exception e)
				{
					throw new ContentLoadException(name, $"unhandled exception ({e.GetType().Name}) when loading raw data: {e.Message}", e);
				}
				finally
				{
					stream?.Dispose();
				}
			}
			else
			{
				var path = _pack.GetDebugItemPath(name);
				if (!File.Exists(path))
					throw new ContentLoadException(name, "the item file does not exist.");

				using (var file = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.None))
				using (var reader = new BinaryReader(file))
				{
					loadDebugItemInfo(name, reader, out var hash, out var realSize, out var ucSize);

					ContentStream stream = null;
					try
					{
						stream = new ContentStream(name, file, realSize, ucSize);
						var ret = stream.ReadBytes(ucSize);
						LastLoadTime = timer.Elapsed;
						return ret;
					}
					catch (Exception e)
					{
						throw new ContentLoadException(name, $"unhandled exception ({e.GetType().Name}) when loading raw data: {e.Message}", e);
					}
					finally
					{
						stream?.Dispose();
					}
				}
			}
		}
		#endregion // Load Functions

		// Performs the actual loading of the content item from the disk
		private object readContentItem(string name, Type type)
		{
			if (_pack.ReleaseMode)
			{
				if (!_pack.TryGetItem(name, out var binNum, out var item))
					throw new ContentLoadException(name, "the item does not exist in the content pack.");
				var loader = getOrCreateLoader(item.LoaderHash);
				if (loader == null)
					throw new ContentLoadException(name, "the item specified a loader that does not exist.");
				if (!loader.ContentType.IsAssignableFrom(type))
					throw new ContentLoadException(name, $"the item loader cannot produce the type '{type.FullName}'.");
				
				object loadedObj = null;
				ContentStream stream = null;
				try
				{
					var fstream = getOrOpenBinStream(binNum);
					stream = new ContentStream(name, fstream, item.Offset, item.RealSize, item.UCSize);
					LoaderContext ctx = new LoaderContext(name, true, stream.Compressed, item.UCSize, type);
					loadedObj = loader.Load(stream, ctx);
				}
				catch (Exception e)
				{
					throw new ContentLoadException(name, $"unhandled exception ({e.GetType().Name}) in loader function: {e.Message}", e);
				}
				finally
				{
					stream?.Dispose();
				}

				if (loadedObj == null)
					throw new ContentLoadException(name, "the loader produced a null value.");

				return loadedObj;
			}
			else
			{
				var path = _pack.GetDebugItemPath(name);
				if (!File.Exists(path))
					throw new ContentLoadException(name, "the item file does not exist.");

				try
				{
					using (var file = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.None))
					using (var reader = new BinaryReader(file))
					{
						loadDebugItemInfo(name, reader, out var hash, out var realSize, out var ucSize);
						var loader = getOrCreateLoader(hash);
						if (loader == null)
							throw new ContentLoadException(name, "the item specified a loader that does not exist.");
						if (!loader.ContentType.IsAssignableFrom(type))
							throw new ContentLoadException(name, $"the item loader cannot produce the type '{type.FullName}'.");

						object loadedObj = null;
						ContentStream stream = null;
						try
						{
							stream = new ContentStream(name, file, realSize, ucSize);
							LoaderContext ctx = new LoaderContext(name, false, false, ucSize, type);
							loadedObj = loader.Load(stream, ctx);
						}
						catch (Exception e)
						{
							throw new ContentLoadException(name, $"unhandled exception ({e.GetType().Name}) in loader function: {e.Message}", e);
						}
						finally
						{
							stream?.Dispose();
						}

						if (loadedObj == null)
							throw new ContentLoadException(name, "the loader produced a null value.");

						return loadedObj;
					}
				}
				catch (ContentLoadException) { throw; }
				catch (Exception e)
				{
					throw new ContentLoadException(name, $"unhandled exception ({e.GetType().Name}) while loading content: {e.Message}", e);
				}
			}
		}

		// Performs validation and header parsing for a .dci file
		private void loadDebugItemInfo(string name, BinaryReader reader, out uint hash, out uint realSize, out uint ucSize)
		{
			var header = reader.ReadBytes(4);
			if (header[0] != 'D' || header[1] != 'C' || header[2] != 'I' || header[3] != 1)
				throw new ContentLoadException(name, "the item file header is invalid.");
			hash = reader.ReadUInt32();
			realSize = reader.ReadUInt32();
			ucSize = reader.ReadUInt32();
		}

		private IContentLoader getOrCreateLoader(uint hash)
		{
			if (_loaders.ContainsKey(hash))
				return _loaders[hash];

			if (!_pack.Loaders.ContainsKey(hash))
				return null;

			var ltype = _pack.Loaders[hash];
			var inst = ltype.CreateInstance();
			_loaders.Add(hash, inst);
			return inst;
		}

		private FileStream getOrOpenBinStream(uint binNum)
		{
			if (_binStreams[(int)binNum] != null)
				return _binStreams[(int)binNum];

			var stream = _pack.BinFiles[(int)binNum].OpenStream();
			_binStreams[(int)binNum] = stream;
			return stream;
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
				{
					Unload();

					if (_pack.ReleaseMode)
					{
						foreach (var bs in _binStreams)
						{
							bs.Close();
							bs.Dispose();
						}
						_binStreams.Clear();
					}
				}

				RemoveManager(this);
			}
			_isDisposed = true;
		}
		#endregion // IDisposable

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
	}
}
